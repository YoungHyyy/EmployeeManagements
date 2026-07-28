using System.Threading.Tasks;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);
        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
        Task MarkAsUsedAsync(int id, string? replacedByTokenHash = null);
        Task RevokeAsync(string tokenHash);
        Task RevokeAllUserTokensAsync(int userId);
    }
}
