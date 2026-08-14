using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EmployeeManagement.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.ExistsByEmailAsync("existing@mail.com", null))
            .ReturnsAsync(true);

        var roleRepository = new Mock<IRoleRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-123456-super-secret-key-123456",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagementClient",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        var tokenService = new TokenService(configuration);

        var service = new AuthService(
            userRepository.Object,
            roleRepository.Object,
            refreshTokenRepository.Object,
            tokenService);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "existing@mail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email đã tồn tại");
        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordsDoNotMatch()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "new@mail.com",
            Password = "Password123!",
            ConfirmPassword = "Different123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không khớp");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmployeeRoleIsMissing()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("new@mail.com")).ReturnsAsync((User?)null);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync((int?)null);

        var service = CreateService(userRepository.Object, refreshTokenRepository: null, roleRepository: roleRepository.Object);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "new@mail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0901234567"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Không tìm thấy role EMPLOYEE");
        userRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        userRepository.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("missing@mail.com")).ReturnsAsync((User?)null);

        var service = CreateService(userRepository.Object);

        var result = await service.LoginAsync(new LoginRequest { Email = "missing@mail.com", Password = "Password123!" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không hợp lệ");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserIsInactive()
    {
        var password = "Password123!";
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("locked@mail.com")).ReturnsAsync(new User
        {
            Id = 7,
            Email = "locked@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = false
        });

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.LoginAsync(new LoginRequest { Email = "locked@mail.com", Password = password });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Tài khoản đã bị vô hiệu hóa");
        result.Data.Should().BeNull();
        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnInvalidCredentials_WhenInactiveUserHasWrongPassword()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("locked@mail.com")).ReturnsAsync(new User
        {
            Id = 7,
            Email = "locked@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsActive = false
        });

        var service = CreateService(userRepository.Object);

        var result = await service.LoginAsync(new LoginRequest { Email = "locked@mail.com", Password = "WrongPassword123!" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không hợp lệ");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFailure_WhenCurrentPasswordIsIncorrect()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!") });

        var service = CreateService(userRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không đúng");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenPasswordIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!") });
        userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(x => x.RevokeAllUserTokensAsync(1)).Returns(Task.CompletedTask);

        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("thành công");
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(1), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldNotRevokeTokens_WhenCurrentPasswordIsIncorrect()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User
        {
            Id = 1,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
        });

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeFalse();
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldRevokeRefreshTokens_WhenOtpIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("user@mail.com")).ReturnsAsync(new User
        {
            Id = 4,
            Email = "user@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
            OtpCode = "123456",
            OtpExpiration = DateTime.UtcNow.AddMinutes(5)
        });
        userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(x => x.RevokeAllUserTokensAsync(4)).Returns(Task.CompletedTask);

        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "user@mail.com",
            OtpCode = "123456",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("thành công");
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(4), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateEmployeeProfile_WithDefaultDepartmentAndPosition()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.ExistsByEmailAsync("new@mail.com", null)).ReturnsAsync(false);
        userRepository.Setup(x => x.CreateWithRoleAsync(It.IsAny<User>(), 2)).ReturnsAsync(11);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync(2);

        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository.Setup(x => x.GetByEmailAsync("new@mail.com")).ReturnsAsync((Employee?)null);
        employeeRepository.Setup(x => x.GetByUserIdAsync(11)).ReturnsAsync((Employee?)null);
        employeeRepository.Setup(x => x.ExistsByEmployeeCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
        employeeRepository.Setup(x => x.CreateAsync(It.IsAny<Employee>())).ReturnsAsync(20);

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(x => x.GetByCodeAsync(DefaultCatalog.DepartmentCode))
            .ReturnsAsync(new Department { Id = 8, Code = DefaultCatalog.DepartmentCode, Name = "Chưa phân bổ" });

        var positions = new Mock<IPositionRepository>();
        positions.Setup(x => x.GetByCodeAsync(DefaultCatalog.PositionCode))
            .ReturnsAsync(new Position { Id = 9, Code = DefaultCatalog.PositionCode, Name = "Nhân viên" });

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var service = CreateService(
            userRepository.Object,
            refreshTokenRepository.Object,
            roleRepository.Object,
            employeeRepository.Object,
            departments.Object,
            positions.Object);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Nguyen Van B",
            Email = "new@mail.com",
            PhoneNumber = "0901234567",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        employeeRepository.Verify(x => x.CreateAsync(It.Is<Employee>(e =>
            e.UserId == 11 &&
            e.Email == "new@mail.com" &&
            e.FullName == "Nguyen Van B" &&
            e.DepartmentId == 8 &&
            e.PositionId == 9 &&
            e.Status == "Working" &&
            e.EmployeeCode.StartsWith("EMP"))), Times.Once);
        employeeRepository.Verify(x => x.LinkUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldLinkExistingEmployee_InsteadOfCreatingDuplicate()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.ExistsByEmailAsync("nv.a@mail.com", null)).ReturnsAsync(false);
        userRepository.Setup(x => x.CreateWithRoleAsync(It.IsAny<User>(), 2)).ReturnsAsync(11);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync(2);

        var employeeRepository = new Mock<IEmployeeRepository>();
        employeeRepository.Setup(x => x.GetByUserIdAsync(11)).ReturnsAsync((Employee?)null);
        employeeRepository.Setup(x => x.GetByEmailAsync("nv.a@mail.com")).ReturnsAsync(new Employee
        {
            Id = 30,
            Email = "nv.a@mail.com",
            UserId = null
        });

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(x => x.GetByCodeAsync(DefaultCatalog.DepartmentCode))
            .ReturnsAsync(new Department { Id = 8, Code = DefaultCatalog.DepartmentCode });
        var positions = new Mock<IPositionRepository>();
        positions.Setup(x => x.GetByCodeAsync(DefaultCatalog.PositionCode))
            .ReturnsAsync(new Position { Id = 9, Code = DefaultCatalog.PositionCode });

        var service = CreateService(
            userRepository.Object,
            new Mock<IRefreshTokenRepository>().Object,
            roleRepository.Object,
            employeeRepository.Object,
            departments.Object,
            positions.Object);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "nv.a@mail.com",
            PhoneNumber = "0901234567",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        result.Success.Should().BeTrue();
        employeeRepository.Verify(x => x.LinkUserAsync(30, 11), Times.Once);
        employeeRepository.Verify(x => x.CreateAsync(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenDefaultCatalogMissing()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.ExistsByEmailAsync("new@mail.com", null)).ReturnsAsync(false);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync(2);

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(x => x.GetByCodeAsync(DefaultCatalog.DepartmentCode)).ReturnsAsync((Department?)null);
        var positions = new Mock<IPositionRepository>();
        positions.Setup(x => x.GetByCodeAsync(DefaultCatalog.PositionCode)).ReturnsAsync((Position?)null);

        var service = CreateService(
            userRepository.Object,
            new Mock<IRefreshTokenRepository>().Object,
            roleRepository.Object,
            new Mock<IEmployeeRepository>().Object,
            departments.Object,
            positions.Object);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Nguyen Van B",
            Email = "new@mail.com",
            PhoneNumber = "0901234567",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("UNASSIGNED");
        userRepository.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    private static AuthService CreateService(
        IUserRepository? userRepository = null,
        IRefreshTokenRepository? refreshTokenRepository = null,
        IRoleRepository? roleRepository = null,
        IEmployeeRepository? employeeRepository = null,
        IDepartmentRepository? departmentRepository = null,
        IPositionRepository? positionRepository = null)
    {
        userRepository ??= new Mock<IUserRepository>().Object;
        roleRepository ??= new Mock<IRoleRepository>().Object;
        refreshTokenRepository ??= new Mock<IRefreshTokenRepository>().Object;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-123456-super-secret-key-123456",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagementClient",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        var tokenService = new TokenService(configuration);
        return new AuthService(
            userRepository,
            roleRepository,
            refreshTokenRepository,
            tokenService,
            employeeRepository: employeeRepository,
            departmentRepository: departmentRepository,
            positionRepository: positionRepository);
    }
}
