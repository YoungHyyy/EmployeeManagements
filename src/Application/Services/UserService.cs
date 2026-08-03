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
            if (!string.IsNullOrWhiteSpace(request.RoleCode) && string.Equals(request.RoleCode, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Chỉ Admin được phép tạo tài khoản Admin");
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

            var roleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? "EMPLOYEE" : request.RoleCode.ToUpperInvariant();
            var roleId = await _roleRepository.GetRoleIdByCodeAsync(roleCode);
            if (!roleId.HasValue)
            {
                roleId = await _roleRepository.GetRoleIdByCodeAsync("EMPLOYEE");
            }

            if (roleId.HasValue)
            {
                await _roleRepository.AssignRoleAsync(id, roleId.Value);
            }

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
