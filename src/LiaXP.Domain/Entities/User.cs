using LiaXP.Domain.Common;

namespace LiaXP.Domain.Entities;

/// <summary>
/// User entity - represents system users with role-based access
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Foreign key to Company (technical key)
    /// </summary>
    public Guid CompanyId { get; private set; }

    /// <summary>
    /// User email (unique per company)
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Hashed password (PBKDF2-HMACSHA256)
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// User role (Admin, Manager, Seller)
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// User active status
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; private set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    // EF Core constructor
    private User() { }

    /// <summary>
    /// Create a new user
    /// </summary>
    public User(
        Guid companyId,
        string email,
        string passwordHash,
        string fullName,
        UserRole role)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("Company ID cannot be empty", nameof(companyId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        CompanyId = companyId;
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role;
        IsActive = true;
    }

    /// <summary>
    /// Update last login timestamp
    /// </summary>
    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Update user password
    /// </summary>
    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash cannot be empty", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    public void UpdateProfile(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
    }
}

/// <summary>
/// User roles enum
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Administrator - full system access
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Manager - can manage sellers and view reports
    /// </summary>
    Manager = 2,

    /// <summary>
    /// Seller - can only view their own data
    /// </summary>
    Seller = 3
}