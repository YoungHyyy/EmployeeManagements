using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Application.Services;

public class AvatarUploadService : IAvatarUploadService
{
    private const long MaxFileSizeInBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];

    public async Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, long fileSize, string webRootPath)
    {
        if (fileStream == null || fileSize == 0)
        {
            throw new ArgumentException("Vui lòng chọn file ảnh hợp lệ.");
        }

        if (fileSize > MaxFileSizeInBytes)
        {
            throw new ArgumentException("Ảnh vượt quá giới hạn 2MB.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Chỉ hỗ trợ định dạng .jpg, .jpeg hoặc .png.");
        }

        if (!AllowedContentTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new ArgumentException("Chỉ hỗ trợ định dạng .jpg, .jpeg hoặc .png.");
        }

        var uploadsFolder = Path.Combine(webRootPath, "uploads", "avatars");
        Directory.CreateDirectory(uploadsFolder);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsFolder, storedFileName);

        await using (var stream = File.Create(fullPath))
        {
            await fileStream.CopyToAsync(stream);
        }

        return $"/uploads/avatars/{storedFileName}";
    }
}
