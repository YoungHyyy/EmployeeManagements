using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>GenericRepository + query đặc thù GetByEmail (Dapper).</summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task<User?> GetByEmailAsync(string email)
        => GetByFieldAsync(nameof(User.Email), email);
}
