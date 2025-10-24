using LiaXP.Domain.Interfaces;
using LiaXP.Domain.Entities;
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

    public async Task<ChatResult> ExecuteAsync(string message, string fromPhone, Guid companyId)
    {
        try
        {
            _logger.LogInformation("Processing chat message from {Phone} for company {CompanyId}", 
                fromPhone, companyId);
            
            // Route the message to determine intent and generate response
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
            
            // Send response back via WhatsApp
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
}

public class ChatResult
{
    public bool Success { get; set; }
    public string Response { get; set; } = string.Empty;
    public string? Intent { get; set; }
    public string? ErrorMessage { get; set; }
}
