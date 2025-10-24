using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ProcessChatMessageUseCase _processChatUseCase;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(ProcessChatMessageUseCase processChatUseCase, ILogger<WebhookController> logger)
    {
        _processChatUseCase = processChatUseCase;
        _logger = logger;
    }

    /// <summary>
    /// WhatsApp webhook - GET for verification (Meta)
    /// </summary>
    [HttpGet("whatsapp")]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string verifyToken,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        var configToken = Environment.GetEnvironmentVariable("META_WA_VERIFY_TOKEN");
        
        if (mode == "subscribe" && verifyToken == configToken)
        {
            _logger.LogInformation("Webhook verified successfully");
            return Ok(challenge);
        }
        
        return Forbid();
    }

    /// <summary>
    /// WhatsApp webhook - POST for messages
    /// </summary>
    [HttpPost("whatsapp")]
    public async Task<IActionResult> ReceiveMessage()
    {
        try
        {
            // Read raw body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            
            _logger.LogInformation("Received webhook: {Body}", body);
            
            // TODO: Parse webhook payload based on provider (Twilio vs Meta)
            // TODO: Extract company, phone, message
            // TODO: Call ProcessChatMessageUseCase
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }
}
