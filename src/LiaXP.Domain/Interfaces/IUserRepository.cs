using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Repository interface for User entity
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by email and company
    /// </summary>
    Task<User?> GetByEmailAndCompanyAsync(
        string email,
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users for a company
    /// </summary>
    Task<IEnumerable<User>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active users for a company
    /// </summary>
    Task<IEnumerable<User>> GetActiveByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new user
    /// </summary>
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing user
    /// </summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete user (soft delete)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user exists by email and company
    /// </summary>
    Task<bool> ExistsByEmailAndCompanyAsync(
        string email,
        Guid companyId,
        CancellationToken cancellationToken = default);
}