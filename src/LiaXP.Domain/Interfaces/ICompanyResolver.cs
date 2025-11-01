
namespace LiaXP.Domain.Interfaces;
/// <summary>
/// Service to resolve Company identifiers
/// Handles conversion between CompanyId (GUID) and CompanyCode (string)
/// Uses in-memory cache for performance
/// </summary>
public interface ICompanyResolver
{
    /// <summary>
    /// Get CompanyId from CompanyCode
    /// </summary>
    Task<Guid?> GetCompanyIdAsync(string companyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get CompanyCode from CompanyId
    /// </summary>
    Task<string?> GetCompanyCodeAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that company exists by Code
    /// </summary>
    Task<bool> ValidateCompanyCodeAsync(string companyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate that company exists by Id
    /// </summary>
    Task<bool> ValidateCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear cache for specific company
    /// </summary>
    void ClearCache(Guid companyId);

    /// <summary>
    /// Clear cache for specific company
    /// </summary>
    void ClearCache(string companyCode);
}