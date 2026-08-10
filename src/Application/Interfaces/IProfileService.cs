using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto?> GetByEmailAsync(string email);
    Task UpdateByEmailAsync(string email, ProfileUpdateRequest request);
    Task<string> UploadAvatarByEmailAsync(
        string email,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string webRootPath);
}
