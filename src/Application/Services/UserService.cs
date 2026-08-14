using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IRefreshTokenRepository? _refreshTokenRepository;
        private readonly IEmployeeRepository? _employeeRepository;

        public UserService(
            IUserRepository userRepo,
            IRoleRepository roleRepository,
            IAuditLogService? auditLogService = null,
            IRefreshTokenRepository? refreshTokenRepository = null,
            IEmployeeRepository? employeeRepository = null)
        {
            _userRepo = userRepo;
            _roleRepository = roleRepository;
            _auditLogService = auditLogService ?? new NoOpAuditLogService();
            _refreshTokenRepository = refreshTokenRepository;
            _employeeRepository = employeeRepository;
        }

        public async Task<UserResponse> CreateAsync(CreateUserRequest request, int? actorUserId = null)
        {
            if (await _userRepo.ExistsByEmailAsync(request.Email))
            {
                throw new InvalidOperationException("Email đã tồn tại");
            }

            var roleCode = string.IsNullOrWhiteSpace(request.RoleCode)
                ? "EMPLOYEE"
                : request.RoleCode.Trim().ToUpperInvariant();

            if (roleCode is not ("ADMIN" or "EMPLOYEE"))
            {
                throw new InvalidOperationException("Role không hợp lệ. Chỉ chấp nhận ADMIN hoặc EMPLOYEE");
            }

            var roleId = await _roleRepository.GetRoleIdByCodeAsync(roleCode);
            if (!roleId.HasValue)
            {
                throw new InvalidOperationException($"Không tìm thấy role {roleCode} trong hệ thống");
            }

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };

            // Users + UserRoles cùng transaction — gán role fail thì không lưu user
            var id = await _userRepo.CreateWithRoleAsync(user, roleId.Value);
            await TryLinkEmployeeByEmailAsync(id, request.Email);

            var created = await _userRepo.GetByIdAsync(id);

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = actorUserId,
                Action = AuditActions.Create,
                Module = AuditModules.User,
                EntityName = "User",
                EntityId = id.ToString(),
                RequestPath = "/api/Users",
                Details = $"Email={request.Email}; Role={roleCode}"
            });

            return Map(created!);
        }

        public async Task DeleteAsync(int id, int? currentUserId = null)
        {
            if (currentUserId.HasValue && currentUserId.Value == id)
            {
                throw new InvalidOperationException("Không thể xóa tài khoản của chính mình");
            }

            var user = await _userRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy người dùng");

            await _userRepo.SoftDeleteAsync(user.Id);

            if (_refreshTokenRepository != null)
            {
                await _refreshTokenRepository.RevokeAllUserTokensAsync(id);
            }

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = currentUserId,
                Action = AuditActions.SoftDelete,
                Module = AuditModules.User,
                EntityName = "User",
                EntityId = id.ToString(),
                RequestPath = $"/api/Users/{id}",
                Details = $"Email={user.Email}"
            });
        }

        public async Task<UserResponse?> GetByIdAsync(int id)
        {
            var u = await _userRepo.GetByIdAsync(id);
            return u == null ? null : Map(u);
        }

        public async Task<IEnumerable<UserResponse>> ListAsync(int page, int pageSize)
        {
            var list = await _userRepo.ListAsync(page, pageSize);
            return list.Select(Map);
        }

        public async Task UpdateAsync(int id, UpdateUserRequest request, int? actorUserId = null)
        {
            var user = await _userRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy người dùng");

            var wasActive = user.IsActive;
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsActive = request.IsActive;
            await _userRepo.UpdateAsync(user);

            if (wasActive && !request.IsActive && _refreshTokenRepository != null)
            {
                await _refreshTokenRepository.RevokeAllUserTokensAsync(id);
            }

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = actorUserId,
                Action = AuditActions.Update,
                Module = AuditModules.User,
                EntityName = "User",
                EntityId = id.ToString(),
                RequestPath = $"/api/Users/{id}"
            });
        }

        private async Task TryLinkEmployeeByEmailAsync(int userId, string email)
        {
            if (_employeeRepository == null)
            {
                return;
            }

            var employee = await _employeeRepository.GetByEmailAsync(email);
            if (employee == null || employee.UserId.HasValue)
            {
                return;
            }

            var alreadyLinked = await _employeeRepository.GetByUserIdAsync(userId);
            if (alreadyLinked != null)
            {
                return;
            }

            await _employeeRepository.LinkUserAsync(employee.Id, userId);
        }

        private static UserResponse Map(User u) => new UserResponse
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FullName,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };

        private sealed class NoOpAuditLogService : IAuditLogService
        {
            public Task WriteAsync(AuditLogWriteRequest request) => Task.CompletedTask;
            public Task<AuditLogListResult> GetListAsync(AuditLogQuery query)
                => Task.FromResult(new AuditLogListResult());
        }
    }
}
