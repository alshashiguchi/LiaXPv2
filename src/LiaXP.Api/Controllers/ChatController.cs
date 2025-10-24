using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ProcessChatMessageUseCase _processChatUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ProcessChatMessageUseCase processChatUseCase, ILogger<ChatController> logger)
    {
        _processChatUseCase = processChatUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Process chat message
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ProcessMessage([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
            return BadRequest(new { error = "Message is required" });

        var companyCode = HttpContext.Items["CompanyCode"]?.ToString();
        if (string.IsNullOrEmpty(companyCode))
            return Unauthorized(new { error = "Company code not found" });

        try
        {
            // TODO: Get actual company ID and seller phone from context
            var companyId = Guid.NewGuid();
            var sellerPhone = User.FindFirst("phone")?.Value ?? "+5511999999999";
            
            var result = await _processChatUseCase.ExecuteAsync(
                request.Message, 
                sellerPhone, 
                companyId);
            
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    response = result.Response,
                    intent = result.Intent
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    error = result.ErrorMessage
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public record ChatRequest(string Message);
