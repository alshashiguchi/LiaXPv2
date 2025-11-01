using LiaXP.Domain.Entities;
using System.Security.Claims;

namespace LiaXP.Domain.Interfaces;

/// <summary>
/// Service for generating and validating JWT tokens
/// IMPORTANT: Tokens include CompanyId (GUID) not CompanyCode (string)
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate JWT token for authenticated user
    /// </summary>
    /// <param name="user">Authenticated user</param>
    /// <param name="companyCode">Company code for display (optional - for backwards compatibility)</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(User user, string? companyCode = null);

    /// <summary>
    /// Validate JWT token
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}