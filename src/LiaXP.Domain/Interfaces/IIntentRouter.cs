using LiaXP.Domain.Enums;

namespace LiaXP.Domain.Interfaces;

public interface IIntentRouter
{
    Task<IntentResult> RouteMessageAsync(string message, Guid companyId, string phoneE164);
}

public class IntentResult
{
    public IntentType Intent { get; set; }
    public string Response { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
