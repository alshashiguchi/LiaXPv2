using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LiaXP.Infrastructure.Services;

public class JwtTokenService : ITokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _signingKey;
    private readonly int _accessTokenTtlMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _issuer = configuration["JWT:Issuer"]
            ?? throw new InvalidOperationException("Issuer not configured");

        _audience = configuration["JWT:Audience"]
            ?? throw new InvalidOperationException("Audience not configured");

        _signingKey = configuration["JWT:SigningKey"]
            ?? throw new InvalidOperationException("SigningKey not configured");

        if (!int.TryParse(configuration["JWT:AccessTokenTTLMinutes"], out _accessTokenTtlMinutes))
        {
            _accessTokenTtlMinutes = 30; // Default 30 minutes
        }

        if (_signingKey.Length < 32)
        {
            throw new InvalidOperationException("SigningKey must be at least 32 characters");
        }
    }

    public string GenerateToken(User user, string? companyCode = null)
    {
        var claims = new List<Claim>
        {
            // User identification
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("user_id", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("email", user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("full_name", user.FullName),
            
            // Company identification - CRITICAL: Use CompanyId (GUID) not CompanyCode
            new Claim("company_id", user.CompanyId.ToString()),
            new Claim("CompanyId", user.CompanyId.ToString()), // Alternative claim name
            
            // Role
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("role", user.Role.ToString()),
            
            // Issued at
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        // OPTIONAL: Include CompanyCode for display purposes only
        // This should NOT be used for authorization logic - use company_id instead
        if (!string.IsNullOrEmpty(companyCode))
        {
            claims.Add(new Claim("company_code", companyCode));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenTtlMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_signingKey);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}