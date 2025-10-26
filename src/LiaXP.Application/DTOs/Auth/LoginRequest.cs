namespace LiaXP.Application.DTOs.Auth;

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
}