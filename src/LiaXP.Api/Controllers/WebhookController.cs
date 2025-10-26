using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;
using LiaXP.Application.DTOs.Webhook;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ProcessChatMessageUseCase _processChatUseCase;
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;

    public WebhookController(ProcessChatMessageUseCase processChatUseCase, ILogger<WebhookController> logger, IConfiguration configuration)
    {
        _processChatUseCase = processChatUseCase;
        _logger = logger;
        _configuration = configuration;

    }

    /// <summary>
    /// Verificação do webhook do WhatsApp (Meta)
    /// </summary>
    [HttpGet("whatsapp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken)
    {
        var expectedToken = _configuration["WhatsApp:VerifyToken"];

        if (mode == "subscribe" && verifyToken == expectedToken)
        {
            _logger.LogInformation("Webhook verificado com sucesso");
            return Ok(challenge);
        }

        _logger.LogWarning("Falha na verificação do webhook");
        return StatusCode(StatusCodes.Status403Forbidden);
    }

    /// <summary>
    /// Recebe mensagens do WhatsApp
    /// </summary>
    [HttpPost("whatsapp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveMessage([FromBody] WhatsAppWebhookRequest request)
    {
        try
        {
            _logger.LogInformation("Webhook recebido: {Object}", request.Object);

            foreach (var entry in request.Entry)
            {
                foreach (var change in entry.Changes)
                {
                    if (change.Field == "messages")
                    {
                        foreach (var message in change.Value.Messages)
                        {
                            _logger.LogInformation(
                                "Mensagem recebida de {From}: {Text}",
                                message.From,
                                message.Text.Body);

                            // TODO: Processar mensagem (identificar vendedor, processar com IA, responder)
                        }
                    }
                }
            }

            return Ok(new { status = "success" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar webhook do WhatsApp");
            return Ok(new { status = "error" }); // Retornar 200 para não retentar
        }
    }
}
