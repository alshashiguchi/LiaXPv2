namespace LiaXP.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string CompanyCode { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FullName { get; private set; }
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() { } // EF Core

    public User(
        string companyCode,
        string email,
        string passwordHash,
        string fullName,
        UserRole role)
    {
        Id = Guid.NewGuid();
        CompanyCode = companyCode ?? throw new ArgumentNullException(nameof(companyCode));
        Email = email?.ToLowerInvariant() ?? throw new ArgumentNullException(nameof(email));
        PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
        FullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Seller = 3
}