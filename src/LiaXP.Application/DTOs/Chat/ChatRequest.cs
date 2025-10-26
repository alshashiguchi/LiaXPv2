namespace LiaXP.Application.DTOs.Chat;

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
}