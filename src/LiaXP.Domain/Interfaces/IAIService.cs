using LiaXP.Domain.Entities;

namespace LiaXP.Domain.Interfaces;

public interface IAIService
{
    Task<AIResponse> ProcessMessageAsync(
        string message,
        string companyCode,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    ChatIntent DetectIntent(string message);
}

public class AIResponse
{
    public string Message { get; set; } = string.Empty;
    public ChatIntent Intent { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}