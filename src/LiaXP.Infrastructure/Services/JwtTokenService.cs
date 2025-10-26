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
    private readonly IConfiguration _configuration;
    private readonly string _signingKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        _signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("JWT SigningKey not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "LiaXP";
        _audience = configuration["Jwt:Audience"] ?? "LiaXP-Api";
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("company_code", user.CompanyCode),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Domain.Interfaces.TokenValidationResult ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_signingKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            return new Domain.Interfaces.TokenValidationResult
            {
                IsValid = true,
                UserId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty),
                CompanyCode = principal.FindFirst("company_code")?.Value,
                Role = principal.FindFirst(ClaimTypes.Role)?.Value
            };
        }
        catch (Exception ex)
        {
            return new Domain.Interfaces.TokenValidationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }
}