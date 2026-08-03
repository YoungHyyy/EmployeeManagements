using Microsoft.AspNetCore.Http;

namespace EmployeeManagement.Api.Services;

public interface IAvatarUploadService
{
    Task<string> SaveAsync(IFormFile file, string webRootPath);
}

public class AvatarUploadService : IAvatarUploadService
{
    private const long MaxFileSizeInBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];

    public async Task<string> SaveAsync(IFormFile file, string webRootPath)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file ảnh hợp lệ.");
        }

        if (file.Length > MaxFileSizeInBytes)
        {
            throw new ArgumentException("Ảnh vượt quá giới hạn 2MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Chỉ hỗ trợ định dạng .jpg, .jpeg hoặc .png.");
        }

        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            throw new ArgumentException("Chỉ hỗ trợ định dạng .jpg, .jpeg hoặc .png.");
        }

        var uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return $"/uploads/avatars/{fileName}";
    }
}
