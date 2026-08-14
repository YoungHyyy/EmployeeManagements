using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IAvatarUploadService _avatarUploadService;
    private readonly IEmployeeRepository? _employeeRepository;
    private readonly IDepartmentRepository? _departmentRepository;
    private readonly IPositionRepository? _positionRepository;

    public ProfileService(
        IUserRepository userRepository,
        IAvatarUploadService avatarUploadService,
        IEmployeeRepository? employeeRepository = null,
        IDepartmentRepository? departmentRepository = null,
        IPositionRepository? positionRepository = null)
    {
        _userRepository = userRepository;
        _avatarUploadService = avatarUploadService;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
    }

    public async Task<ProfileDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return null;
        }

        var employee = await FindAndLinkEmployeeAsync(user);
        return await MapAsync(user, employee);
    }

    public async Task UpdateByEmailAsync(string email, ProfileUpdateRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ người dùng");

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        await _userRepository.UpdateAsync(user);

        var employee = await FindAndLinkEmployeeAsync(user);
        if (employee == null)
        {
            return;
        }

        employee.FullName = request.FullName;
        employee.PhoneNumber = request.PhoneNumber;
        employee.DateOfBirth = request.DateOfBirth;
        employee.Gender = request.Gender;
        employee.Address = request.Address;
        await _employeeRepository!.UpdateAsync(employee);
    }

    public async Task<string> UploadAvatarByEmailAsync(
        string email,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string webRootPath)
    {
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ người dùng");

        if (fileStream is null || fileSize <= 0 || string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Vui lòng chọn file ảnh hợp lệ.");
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

        var employee = await FindAndLinkEmployeeAsync(user);
        if (employee != null)
        {
            employee.AvatarUrl = avatarPath;
            await _employeeRepository!.UpdateAsync(employee);
        }

        return avatarPath;
    }

    private async Task<Employee?> FindAndLinkEmployeeAsync(User user)
    {
        if (_employeeRepository == null)
        {
            return null;
        }

        var byUser = await _employeeRepository.GetByUserIdAsync(user.Id);
        if (byUser != null)
        {
            return byUser;
        }

        var byEmail = await _employeeRepository.GetByEmailAsync(user.Email);
        if (byEmail == null)
        {
            return null;
        }

        if (!byEmail.UserId.HasValue)
        {
            await _employeeRepository.LinkUserAsync(byEmail.Id, user.Id);
            byEmail.UserId = user.Id;
        }

        return byEmail;
    }

    private async Task<ProfileDto> MapAsync(User user, Employee? employee)
    {
        string? departmentName = null;
        string? positionName = null;
        if (employee != null && _departmentRepository != null)
        {
            departmentName = (await _departmentRepository.GetByIdAsync(employee.DepartmentId))?.Name;
        }

        if (employee != null && _positionRepository != null)
        {
            positionName = (await _positionRepository.GetByIdAsync(employee.PositionId))?.Name;
        }

        return new ProfileDto
        {
            Id = user.Id,
            EmployeeId = employee?.Id,
            EmployeeCode = employee?.EmployeeCode,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl ?? employee?.AvatarUrl,
            DateOfBirth = employee?.DateOfBirth,
            Gender = employee?.Gender,
            Address = employee?.Address,
            DepartmentId = employee?.DepartmentId,
            PositionId = employee?.PositionId,
            DepartmentName = departmentName,
            PositionName = positionName,
            HireDate = employee?.HireDate,
            Status = employee?.Status,
            CreatedAt = user.CreatedAt
        };
    }
}
