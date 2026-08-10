using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepository;

        public UserService(IUserRepository userRepo, IRoleRepository roleRepository)
        {
            _userRepo = userRepo;
            _roleRepository = roleRepository;
        }

        public async Task<UserResponse> CreateAsync(CreateUserRequest request)
        {
            // Role assignment is only reachable via Admin-only API (/api/users).
            // Public register never uses this path and always forces EMPLOYEE.
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
            return Map(created!);
        }

        public async Task DeleteAsync(int id)
        {
            await _userRepo.SoftDeleteAsync(id);
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

        public async Task UpdateAsync(int id, UpdateUserRequest request)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return;
            user.FullName = request.FullName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsActive = request.IsActive;
            await _userRepo.UpdateAsync(user);
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
    }
    
}
