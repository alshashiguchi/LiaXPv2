namespace LiaXP.Application.DTOs.Auth;

/// <summary>
/// Login response DTO
/// Includes JWT token and user information
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Authenticated user information
    /// </summary>
    public UserInfo User { get; set; } = new();
}
