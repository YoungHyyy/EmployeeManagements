namespace EmployeeManagement.Application.Interfaces;

public interface IAvatarUploadService
{
    Task<string> SaveAsync(Stream fileStream, string fileName, string contentType, long fileSize, string webRootPath);
}
