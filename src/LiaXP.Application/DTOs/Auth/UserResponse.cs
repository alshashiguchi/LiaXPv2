namespace LiaXP.Application.DTOs.Auth;

/// <summary>
/// User response DTO
/// Includes BOTH CompanyId (technical) AND CompanyCode (display)
/// </summary>
public class UserResponse
{
    /// <summary>
    /// User unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Company technical identifier (GUID)
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company business code (e.g., "ACME")
    /// For display purposes - more user-friendly than GUID
    /// </summary>
    public string? CompanyCode { get; set; }

    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}