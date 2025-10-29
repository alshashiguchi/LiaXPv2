using LiaXP.Domain.Enums; 

namespace LiaXP.Domain.Interfaces;

public interface IAIService
{
    Task<AIResponse> ProcessMessageAsync(
        string message,
        string companyCode,
        Dictionary<string, object>? context = null,
        CancellationToken cancellationToken = default);

    IntentType DetectIntent(string message);
}

public class AIResponse
{
    public string Message { get; set; } = string.Empty;
    public IntentType Intent { get; set; } 
    public Dictionary<string, string> Metadata { get; set; } = new();
}