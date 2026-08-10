using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Application.Services;

public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IAvatarUploadService _avatarUploadService;

    public ProfileService(IUserRepository userRepository, IAvatarUploadService avatarUploadService)
    {
        _userRepository = userRepository;
        _avatarUploadService = avatarUploadService;
    }

    public async Task<ProfileDto?> GetByEmailAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user == null ? null : Map(user);
    }

    public async Task UpdateByEmailAsync(string email, ProfileUpdateRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(email)
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ người dùng");

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        await _userRepository.UpdateAsync(user);
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
        return avatarPath;
    }

    private static ProfileDto Map(Domain.Entities.User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        AvatarUrl = user.AvatarUrl,
        CreatedAt = user.CreatedAt
    };
}
