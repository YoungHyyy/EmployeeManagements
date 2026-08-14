namespace EmployeeManagement.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(int userId, string email, string roleCode);
    int GetAccessTokenExpiresMinutes();
    int GetRefreshTokenDays();
}
