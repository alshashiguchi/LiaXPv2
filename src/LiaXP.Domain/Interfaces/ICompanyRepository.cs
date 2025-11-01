using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Repository interface for Company aggregate
/// Supports queries by both technical ID and business Code
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Get company by technical ID (GUID)
    /// </summary>
    /// <param name="id">Company unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company or null if not found</returns>
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get company by business code (string)
    /// </summary>
    /// <param name="code">Company business code (e.g., "ACME")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company or null if not found</returns>
    Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if company exists by ID
    /// </summary>
    Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if company exists by Code
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active companies
    /// </summary>
    Task<IEnumerable<Company>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new company
    /// </summary>
    Task<Company> AddAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing company
    /// </summary>
    Task<Company> UpdateAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete company (soft delete)
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}