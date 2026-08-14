using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IProfileService
{
    /// <summary>Hồ sơ nhân viên của user đang đăng nhập — xác định bằng UserId từ JWT, không nhận EmployeeId từ client.</summary>
    Task<ProfileDto?> GetByUserIdAsync(int userId);
    Task UpdateByUserIdAsync(int userId, ProfileUpdateRequest request);
    Task<string> UploadAvatarByUserIdAsync(
        int userId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string webRootPath);
}
