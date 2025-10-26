namespace LiaXP.Application.DTOs.Auth;

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; } = 3600;
    public UserInfo User { get; init; } = default!;
}

public record UserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
}