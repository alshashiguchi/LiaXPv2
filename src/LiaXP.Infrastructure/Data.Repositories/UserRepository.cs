using Dapper;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace LiaXP.Infrastructure.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly string _connectionString;

    public UserRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT 
                Id, CompanyCode, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, LastLoginAt
            FROM Users
            WHERE Email = @Email 
                AND CompanyCode = @CompanyCode 
                AND IsActive = 1";

        var result = await connection.QueryFirstOrDefaultAsync<UserDto>(
            new CommandDefinition(sql, new { Email = email, CompanyCode = companyCode }, cancellationToken: cancellationToken));

        return result?.ToEntity();
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            SELECT 
                Id, CompanyCode, Email, PasswordHash, FullName, 
                Role, IsActive, CreatedAt, LastLoginAt
            FROM Users
            WHERE Id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<UserDto>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return result?.ToEntity();
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            INSERT INTO Users (Id, CompanyCode, Email, PasswordHash, FullName, Role, IsActive, CreatedAt)
            VALUES (@Id, @CompanyCode, @Email, @PasswordHash, @FullName, @Role, @IsActive, @CreatedAt)";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                user.Id,
                user.CompanyCode,
                user.Email,
                user.PasswordHash,
                user.FullName,
                Role = (int)user.Role,
                user.IsActive,
                user.CreatedAt
            }, cancellationToken: cancellationToken));

        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(_connectionString);

        const string sql = @"
            UPDATE Users 
            SET LastLoginAt = @LastLoginAt,
                IsActive = @IsActive
            WHERE Id = @Id";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                user.Id,
                user.LastLoginAt,
                user.IsActive
            }, cancellationToken: cancellationToken));
    }

    private class UserDto
    {
        public Guid Id { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public User ToEntity()
        {
            // Use reflection para criar instância privada
            var user = (User)Activator.CreateInstance(typeof(User), true)!;
            typeof(User).GetProperty(nameof(Id))!.SetValue(user, Id);
            typeof(User).GetProperty(nameof(CompanyCode))!.SetValue(user, CompanyCode);
            typeof(User).GetProperty(nameof(Email))!.SetValue(user, Email);
            typeof(User).GetProperty(nameof(PasswordHash))!.SetValue(user, PasswordHash);
            typeof(User).GetProperty(nameof(FullName))!.SetValue(user, FullName);
            typeof(User).GetProperty(nameof(Role))!.SetValue(user, (UserRole)Role);
            typeof(User).GetProperty(nameof(IsActive))!.SetValue(user, IsActive);
            typeof(User).GetProperty(nameof(CreatedAt))!.SetValue(user, CreatedAt);
            typeof(User).GetProperty(nameof(LastLoginAt))!.SetValue(user, LastLoginAt);
            return user;
        }
    }
}