using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAvatarUploadService _avatarUploadService;

    public ProfileService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IAvatarUploadService avatarUploadService)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _avatarUploadService = avatarUploadService;
    }

    public async Task<ProfileDto?> GetByUserIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var employee = await _employeeRepository.GetByUserIdAsync(userId);
        return Map(user, employee);
    }

    public async Task UpdateByUserIdAsync(int userId, ProfileUpdateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException(ApiMessages.ProfileNotFound);

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        await _userRepository.UpdateAsync(user);

        var employee = await _employeeRepository.GetByUserIdAsync(userId);
        if (employee != null)
        {
            employee.FullName = request.FullName;
            employee.PhoneNumber = request.PhoneNumber;
            employee.DateOfBirth = request.DateOfBirth;
            employee.Gender = request.Gender;
            employee.Address = request.Address;
            await _employeeRepository.UpdateAsync(employee);
        }
    }

    public async Task<string> UploadAvatarByUserIdAsync(
        int userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string webRootPath)
    {
        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException(ApiMessages.ProfileNotFound);

        if (fileStream is null || fileSize <= 0 || string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException(ApiMessages.AvatarInvalid);
        }

        var storageRoot = string.IsNullOrWhiteSpace(webRootPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
            : webRootPath;

        var avatarPath = await _avatarUploadService.SaveAsync(
            fileStream,
            fileName,
            contentType,
            fileSize,
            storageRoot);

        user.AvatarUrl = avatarPath;
        await _userRepository.UpdateAsync(user);
        return avatarPath;
    }

    private static ProfileDto Map(User user, Employee? employee) => new()
    {
        Id = user.Id,
        EmployeeId = employee?.Id ?? 0,
        EmployeeCode = employee?.EmployeeCode ?? string.Empty,
        FullName = employee?.FullName ?? user.FullName,
        Email = user.Email,
        PhoneNumber = employee?.PhoneNumber ?? user.PhoneNumber,
        DateOfBirth = employee?.DateOfBirth,
        Gender = employee?.Gender,
        Address = employee?.Address,
        DepartmentId = employee?.DepartmentId ?? 0,
        PositionId = employee?.PositionId ?? 0,
        HireDate = employee?.HireDate ?? default,
        Status = employee?.Status,
        AvatarUrl = user.AvatarUrl,
        CreatedAt = user.CreatedAt
    };
}
