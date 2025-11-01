using LiaXP.Domain.Entities;

namespace LiaXP.Application.DTOs.Auth;

/// <summary>
/// Create user request DTO
/// Accepts EITHER CompanyId (GUID) OR CompanyCode (string)
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// Company technical identifier (GUID)
    /// Use this if you know the internal CompanyId
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Company business code (e.g., "ACME", "CONTOSO")
    /// Use this for user-friendly API calls
    /// Will be converted to CompanyId internally
    /// </summary>
    public string? CompanyCode { get; set; }

    /// <summary>
    /// User email address (must be unique per company)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (will be hashed)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User role (Admin = 1, Manager = 2, Seller = 3)
    /// </summary>
    public UserRole Role { get; set; }
}