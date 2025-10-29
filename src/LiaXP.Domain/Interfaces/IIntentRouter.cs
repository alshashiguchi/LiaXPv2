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
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public double? Confidence { get; set; }
    public long? ProcessingTimeMs { get; set; }

    public static IntentResult Error(string errorMessage, long? processingTimeMs = null)
    {
        return new IntentResult
        {
            Intent = IntentType.Unknown,
            Response = "Desculpe, ocorreu um erro ao processar sua mensagem.",
            Success = false,
            ErrorMessage = errorMessage,
            Confidence = 0.0,
            ProcessingTimeMs = processingTimeMs
        };
    }

    public static IntentResult UserError(string userMessage, string internalError, long? processingTimeMs = null)
    {
        return new IntentResult
        {
            Intent = IntentType.Unknown,
            Response = userMessage, // Mensagem amigável para o usuário
            Success = false,
            ErrorMessage = internalError, // Mensagem técnica para logs
            Confidence = 0.0,
            ProcessingTimeMs = processingTimeMs
        };
    }

    public static IntentResult SuccessResponse(
        IntentType intent,
        string response,
        double confidence,
        long processingTimeMs)
    {
        return new IntentResult
        {
            Intent = intent,
            Response = response,
            Success = true,
            Confidence = confidence,
            ProcessingTimeMs = processingTimeMs
        };
    }
}
