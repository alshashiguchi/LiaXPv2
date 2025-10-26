using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, string companyCode, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}