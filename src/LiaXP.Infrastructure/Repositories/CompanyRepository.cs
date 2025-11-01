using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Repositories;

/// <summary>
/// Company repository implementation using Dapper
/// </summary>
public class CompanyRepository : ICompanyRepository
{
    private readonly string _connectionString;
    private readonly ILogger<CompanyRepository> _logger;

    public CompanyRepository(
        IConfiguration configuration,
        ILogger<CompanyRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Code, Name, Description, IsActive, CreatedAt, UpdatedAt, IsDeleted
            FROM Company
            WHERE Id = @Id AND IsDeleted = 0";

        using var connection = new SqlConnection(_connectionString);
        var company = await connection.QuerySingleOrDefaultAsync<Company>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return company;
    }

    public async Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        const string sql = @"
            SELECT Id, Code, Name, Description, IsActive, CreatedAt, UpdatedAt, IsDeleted
            FROM Company
            WHERE Code = @Code AND IsDeleted = 0";

        using var connection = new SqlConnection(_connectionString);
        var company = await connection.QuerySingleOrDefaultAsync<Company>(
            new CommandDefinition(sql, new { Code = code.ToUpperInvariant() }, cancellationToken: cancellationToken));

        return company;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CAST(CASE WHEN EXISTS(
                SELECT 1 FROM Company WHERE Id = @Id AND IsDeleted = 0
            ) THEN 1 ELSE 0 END AS BIT)";

        using var connection = new SqlConnection(_connectionString);
        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return exists;
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        const string sql = @"
            SELECT CAST(CASE WHEN EXISTS(
                SELECT 1 FROM Company WHERE Code = @Code AND IsDeleted = 0
            ) THEN 1 ELSE 0 END AS BIT)";

        using var connection = new SqlConnection(_connectionString);
        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Code = code.ToUpperInvariant() }, cancellationToken: cancellationToken));

        return exists;
    }

    public async Task<IEnumerable<Company>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT Id, Code, Name, Description, IsActive, CreatedAt, UpdatedAt, IsDeleted
            FROM Company
            WHERE IsActive = 1 AND IsDeleted = 0
            ORDER BY Name";

        using var connection = new SqlConnection(_connectionString);
        var companies = await connection.QueryAsync<Company>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return companies;
    }

    public async Task<Company> AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Company (Id, Code, Name, Description, IsActive, CreatedAt, IsDeleted)
            VALUES (@Id, @Code, @Name, @Description, @IsActive, GETUTCDATE(), 0);
            
            SELECT Id, Code, Name, Description, IsActive, CreatedAt, UpdatedAt, IsDeleted
            FROM Company
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);

        try
        {
            var inserted = await connection.QuerySingleAsync<Company>(
                new CommandDefinition(sql, company, cancellationToken: cancellationToken));

            _logger.LogInformation(
                "Company created | Id: {Id} | Code: {Code}",
                company.Id,
                company.Code);

            return inserted;
        }
        catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
        {
            _logger.LogWarning(
                "Duplicate company code | Code: {Code}",
                company.Code);

            throw new InvalidOperationException($"Company with code '{company.Code}' already exists", ex);
        }
    }

    public async Task<Company> UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Company
            SET Name = @Name,
                Description = @Description,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0;
            
            SELECT Id, Code, Name, Description, IsActive, CreatedAt, UpdatedAt, IsDeleted
            FROM Company
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        var updated = await connection.QuerySingleOrDefaultAsync<Company>(
            new CommandDefinition(sql, company, cancellationToken: cancellationToken));

        if (updated == null)
        {
            throw new InvalidOperationException($"Company with Id '{company.Id}' not found");
        }

        _logger.LogInformation(
            "Company updated | Id: {Id} | Code: {Code}",
            company.Id,
            company.Code);

        return updated;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Company
            SET IsDeleted = 1,
                IsActive = 0,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0";

        using var connection = new SqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Company with Id '{id}' not found");
        }

        _logger.LogInformation(
            "Company deleted (soft) | Id: {Id}",
            id);
    }
}