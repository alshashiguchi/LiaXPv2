using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Services;

public class CompanyResolver : ICompanyResolver
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CompanyResolver> _logger;

    private const int CacheExpirationMinutes = 30;
    private const string CacheKeyPrefixId = "company:id:";
    private const string CacheKeyPrefixCode = "company:code:";

    public CompanyResolver(
        ICompanyRepository companyRepository,
        IMemoryCache cache,
        ILogger<CompanyResolver> logger)
    {
        _companyRepository = companyRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid?> GetCompanyIdAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
            return null;

        var normalizedCode = companyCode.ToUpperInvariant();
        var cacheKey = $"{CacheKeyPrefixCode}{normalizedCode}";

        // Try get from cache
        if (_cache.TryGetValue<Guid>(cacheKey, out var cachedId))
        {
            return cachedId;
        }

        // Fetch from database
        var company = await _companyRepository.GetByCodeAsync(normalizedCode, cancellationToken);

        if (company == null)
        {
            _logger.LogWarning(
                "Company not found | Code: {Code}",
                normalizedCode);
            return null;
        }

        // Cache both directions
        CacheCompany(company.Id, company.Code);

        return company.Id;
    }

    public async Task<string?> GetCompanyCodeAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return null;

        var cacheKey = $"{CacheKeyPrefixId}{companyId}";

        // Try get from cache
        if (_cache.TryGetValue<string>(cacheKey, out var cachedCode))
        {
            return cachedCode;
        }

        // Fetch from database
        var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);

        if (company == null)
        {
            _logger.LogWarning(
                "Company not found | Id: {Id}",
                companyId);
            return null;
        }

        // Cache both directions
        CacheCompany(company.Id, company.Code);

        return company.Code;
    }

    public async Task<bool> ValidateCompanyCodeAsync(string companyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
            return false;

        var normalizedCode = companyCode.ToUpperInvariant();
        var cacheKey = $"{CacheKeyPrefixCode}{normalizedCode}";

        // Check cache first
        if (_cache.TryGetValue<Guid>(cacheKey, out _))
        {
            return true;
        }

        // Check database
        var exists = await _companyRepository.ExistsByCodeAsync(normalizedCode, cancellationToken);

        if (exists)
        {
            // Load company to populate cache
            var company = await _companyRepository.GetByCodeAsync(normalizedCode, cancellationToken);
            if (company != null)
            {
                CacheCompany(company.Id, company.Code);
            }
        }

        return exists;
    }

    public async Task<bool> ValidateCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
            return false;

        var cacheKey = $"{CacheKeyPrefixId}{companyId}";

        // Check cache first
        if (_cache.TryGetValue<string>(cacheKey, out _))
        {
            return true;
        }

        // Check database
        var exists = await _companyRepository.ExistsByIdAsync(companyId, cancellationToken);

        if (exists)
        {
            // Load company to populate cache
            var company = await _companyRepository.GetByIdAsync(companyId, cancellationToken);
            if (company != null)
            {
                CacheCompany(company.Id, company.Code);
            }
        }

        return exists;
    }

    public void ClearCache(Guid companyId)
    {
        var cacheKey = $"{CacheKeyPrefixId}{companyId}";
        _cache.Remove(cacheKey);

        _logger.LogDebug(
            "Cache cleared | CompanyId: {CompanyId}",
            companyId);
    }

    public void ClearCache(string companyCode)
    {
        if (string.IsNullOrWhiteSpace(companyCode))
            return;

        var normalizedCode = companyCode.ToUpperInvariant();
        var cacheKey = $"{CacheKeyPrefixCode}{normalizedCode}";
        _cache.Remove(cacheKey);

        _logger.LogDebug(
            "Cache cleared | CompanyCode: {CompanyCode}",
            normalizedCode);
    }

    private void CacheCompany(Guid companyId, string companyCode)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes))
            .SetPriority(CacheItemPriority.Normal);

        // Cache Id -> Code
        _cache.Set($"{CacheKeyPrefixId}{companyId}", companyCode, options);

        // Cache Code -> Id
        _cache.Set($"{CacheKeyPrefixCode}{companyCode.ToUpperInvariant()}", companyId, options);
    }
}