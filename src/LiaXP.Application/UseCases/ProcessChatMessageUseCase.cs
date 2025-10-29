using LiaXP.Domain.Interfaces;
using LiaXP.Application.DTOs.Chat;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases;

public class ProcessChatMessageUseCase
{
    private readonly IIntentRouter _intentRouter;
    private readonly IWhatsAppClient _whatsAppClient;
    private readonly ISalesDataSource _salesDataSource;
    private readonly ILogger<ProcessChatMessageUseCase> _logger;

    public ProcessChatMessageUseCase(
        IIntentRouter intentRouter,
        IWhatsAppClient whatsAppClient,
        ISalesDataSource salesDataSource,
        ILogger<ProcessChatMessageUseCase> logger)
    {
        _intentRouter = intentRouter;
        _whatsAppClient = whatsAppClient;
        _salesDataSource = salesDataSource;
        _logger = logger;
    }

    // Método antigo mantido para compatibilidade
    public async Task<ChatResult> ExecuteAsync(string message, string fromPhone, Guid companyId)
    {
        try
        {
            _logger.LogInformation("Processing chat message from {Phone} for company {CompanyId}",
                fromPhone, companyId);

            var intentResult = await _intentRouter.RouteMessageAsync(message, companyId, fromPhone);

            if (!intentResult.Success)
            {
                _logger.LogWarning("Intent routing failed: {Error}", intentResult.ErrorMessage);
                return new ChatResult
                {
                    Success = false,
                    ErrorMessage = intentResult.ErrorMessage
                };
            }

            var sendResult = await _whatsAppClient.SendMessageAsync(fromPhone, intentResult.Response, companyId);

            if (!sendResult.Success)
            {
                _logger.LogError("Failed to send WhatsApp message: {Error}", sendResult.ErrorMessage);
                return new ChatResult
                {
                    Success = false,
                    ErrorMessage = sendResult.ErrorMessage
                };
            }

            _logger.LogInformation("Successfully processed and responded to chat message");

            return new ChatResult
            {
                Success = true,
                Response = intentResult.Response,
                Intent = intentResult.Intent.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return new ChatResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    // Novo método para o controller
    public async Task<Result<ChatResponse>> ExecuteAsync(
        ChatRequest request,
        Guid userId,
        string companyCode,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Processing chat message from user {UserId} for company {CompanyCode}",
                userId, companyCode);

            // Buscar o telefone do usuário através de outras formas
            // Como não temos GetSellerByIdAsync, vamos usar uma abordagem alternativa
            var companyGuid = Guid.Parse(companyCode);

            // Processar a intenção diretamente (assumindo que o usuário está autenticado)
            var intentResult = await _intentRouter.RouteMessageAsync(
                request.Message,
                companyGuid,
                "+5511999999999"); // Você precisará obter o telefone de outra forma

            if (!intentResult.Success)
            {
                _logger.LogWarning("Intent routing failed: {Error}", intentResult.ErrorMessage);
                return Result<ChatResponse>.Failure(
                    intentResult.ErrorMessage ?? "Erro ao processar mensagem");
            }

            _logger.LogInformation(
                "Successfully processed chat message with intent {Intent} and confidence {Confidence}",
                intentResult.Intent,
                intentResult.Confidence);

            var response = new ChatResponse
            {
                Message = intentResult.Response,
                Intent = intentResult.Intent.ToString(),
                Confidence = intentResult.Confidence ?? 0.0,
                ProcessingTimeMs = intentResult.ProcessingTimeMs ?? 0,
                Timestamp = DateTime.UtcNow
            };

            return Result<ChatResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for user {UserId}", userId);
            return Result<ChatResponse>.Failure($"Erro interno: {ex.Message}");
        }
    }
}

public class ChatResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? ErrorMessage { get; set; }
}