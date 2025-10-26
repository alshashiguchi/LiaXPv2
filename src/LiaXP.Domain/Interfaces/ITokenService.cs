using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    TokenValidationResult ValidateToken(string token);
}

public class TokenValidationResult
{
    public bool IsValid { get; set; }
    public Guid? UserId { get; set; }
    public string? CompanyCode { get; set; }
    public string? Role { get; set; }
    public string? ErrorMessage { get; set; }
}