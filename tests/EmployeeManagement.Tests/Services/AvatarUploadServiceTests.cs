using EmployeeManagement.Application.Services;
using FluentAssertions;

namespace EmployeeManagement.Tests.Services;

public class AvatarUploadServiceTests
{
    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenFileIsEmpty()
    {
        var service = new AvatarUploadService();

        var act = async () => await service.SaveAsync(new MemoryStream(), "avatar.png", "image/png", 0, "/tmp");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Vui lòng chọn file ảnh hợp lệ.");
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenFileSizeExceeds2MB()
    {
        var service = new AvatarUploadService();
        var stream = new MemoryStream(new byte[2 * 1024 * 1024 + 1]);

        var act = async () => await service.SaveAsync(stream, "avatar.png", "image/png", stream.Length, "/tmp");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Ảnh vượt quá giới hạn 2MB.");
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenExtensionIsNotAllowed()
    {
        var service = new AvatarUploadService();
        var stream = new MemoryStream(new byte[100]);

        var act = async () => await service.SaveAsync(stream, "avatar.gif", "image/gif", 100, "/tmp");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Chỉ hỗ trợ định dạng .jpg, .jpeg hoặc .png.");
    }
}
