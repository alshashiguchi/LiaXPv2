using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiaXP.Infrastructure.Repositories;

/// <summary>
/// User repository implementation using Dapper
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly string _connectionString;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        IConfiguration configuration,
        ILogger<UserRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, UpdatedAt, LastLoginAt, IsDeleted
            FROM Users
            WHERE Id = @Id AND IsDeleted = 0";

        using var connection = new SqlConnection(_connectionString);
        var user = await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return user;
    }

    public async Task<User?> GetByEmailAndCompanyAsync(
        string email,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, UpdatedAt, LastLoginAt, IsDeleted
            FROM Users
            WHERE Email = @Email 
              AND CompanyId = @CompanyId 
              AND IsDeleted = 0";

        using var connection = new SqlConnection(_connectionString);
        var user = await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(
                sql,
                new { Email = email.ToLowerInvariant(), CompanyId = companyId },
                cancellationToken: cancellationToken));

        return user;
    }

    public async Task<IEnumerable<User>> GetByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, UpdatedAt, LastLoginAt, IsDeleted
            FROM Users
            WHERE CompanyId = @CompanyId AND IsDeleted = 0
            ORDER BY FullName";

        using var connection = new SqlConnection(_connectionString);
        var users = await connection.QueryAsync<User>(
            new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken));

        return users;
    }

    public async Task<IEnumerable<User>> GetActiveByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, UpdatedAt, LastLoginAt, IsDeleted
            FROM Users
            WHERE CompanyId = @CompanyId 
              AND IsActive = 1 
              AND IsDeleted = 0
            ORDER BY FullName";

        using var connection = new SqlConnection(_connectionString);
        var users = await connection.QueryAsync<User>(
            new CommandDefinition(sql, new { CompanyId = companyId }, cancellationToken: cancellationToken));

        return users;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Users (
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, IsDeleted
            )
            VALUES (
                @Id, @CompanyId, @Email, @PasswordHash, @FullName, 
                @Role, @IsActive, GETUTCDATE(), 0
            );
            
            SELECT 
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, UpdatedAt, LastLoginAt, IsDeleted
            FROM Users
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);

        try
        {
            var inserted = await connection.QuerySingleAsync<User>(
                new CommandDefinition(sql, user, cancellationToken: cancellationToken));

            _logger.LogInformation(
                "User created | UserId: {UserId} | Email: {Email} | CompanyId: {CompanyId}",
                user.Id,
                user.Email,
                user.CompanyId);

            return inserted;
        }
        catch (SqlException ex) when (ex.Number == 2627) // Unique constraint violation
        {
            _logger.LogWarning(
                "Duplicate user | Email: {Email} | CompanyId: {CompanyId}",
                user.Email,
                user.CompanyId);

            throw new InvalidOperationException(
                $"User with email '{user.Email}' already exists for this company", ex);
        }
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Users
            SET Email = @Email,
                FullName = @FullName,
                Role = @Role,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE(),
                LastLoginAt = @LastLoginAt
            WHERE Id = @Id AND IsDeleted = 0;
            
            SELECT 
                Id, CompanyId, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, UpdatedAt, LastLoginAt, IsDeleted
            FROM Users
            WHERE Id = @Id";

        using var connection = new SqlConnection(_connectionString);
        var updated = await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, user, cancellationToken: cancellationToken));

        if (updated == null)
        {
            throw new InvalidOperationException($"User with Id '{user.Id}' not found");
        }

        _logger.LogInformation(
            "User updated | UserId: {UserId} | Email: {Email}",
            user.Id,
            user.Email);

        return updated;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Users
            SET IsDeleted = 1,
                IsActive = 0,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND IsDeleted = 0";

        using var connection = new SqlConnection(_connectionString);
        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"User with Id '{id}' not found");
        }

        _logger.LogInformation(
            "User deleted (soft) | UserId: {UserId}",
            id);
    }

    public async Task<bool> ExistsByEmailAndCompanyAsync(
        string email,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT CAST(CASE WHEN EXISTS(
                SELECT 1 FROM Users 
                WHERE Email = @Email 
                  AND CompanyId = @CompanyId 
                  AND IsDeleted = 0
            ) THEN 1 ELSE 0 END AS BIT)";

        using var connection = new SqlConnection(_connectionString);
        var exists = await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                sql,
                new { Email = email.ToLowerInvariant(), CompanyId = companyId },
                cancellationToken: cancellationToken));

        return exists;
    }
}