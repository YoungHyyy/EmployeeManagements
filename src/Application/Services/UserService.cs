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

        public UserService(
            IUserRepository userRepo,
            IRoleRepository roleRepository,
            IAuditLogService? auditLogService = null)
        {
            _userRepo = userRepo;
            _roleRepository = roleRepository;
            _auditLogService = auditLogService ?? new NoOpAuditLogService();
        }

        public async Task<UserResponse> CreateAsync(CreateUserRequest request, int? actorUserId = null)
        {
            var existing = await _userRepo.GetByEmailAsync(request.Email);
            if (existing != null)
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

            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };
            var id = await _userRepo.CreateAsync(user);

            var roleId = await _roleRepository.GetRoleIdByCodeAsync(roleCode);
            if (!roleId.HasValue)
            {
                throw new InvalidOperationException($"Không tìm thấy role {roleCode} trong hệ thống");
            }

            await _roleRepository.AssignRoleAsync(id, roleId.Value);

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

            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsActive = request.IsActive;
            await _userRepo.UpdateAsync(user);

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
