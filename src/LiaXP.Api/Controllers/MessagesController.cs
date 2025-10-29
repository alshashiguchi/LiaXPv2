using LiaXP.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("messages")]
[Produces("application/json")]
public class MessagesController : ControllerBase
{
    private readonly IMessageLogRepository _messageLogRepository;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(
        IMessageLogRepository messageLogRepository,
        ILogger<MessagesController> logger)
    {
        _messageLogRepository = messageLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get message logs for the company
    /// </summary>
    [HttpGet("logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int limit = 100)
    {
        var companyId = GetCompanyIdFromClaims(); // Helper method

        var logs = await _messageLogRepository.GetByCompanyAsync(
            companyId,
            startDate,
            endDate,
            limit
        );

        return Ok(logs);
    }

    /// <summary>
    /// Get conversation history with a specific phone
    /// </summary>
    [HttpGet("conversation/{phoneE164}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversation(
        string phoneE164,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var logs = await _messageLogRepository.GetByPhoneAsync(
            phoneE164,
            startDate,
            endDate,
            limit: 50
        );

        return Ok(logs);
    }

    /// <summary>
    /// Get failed messages for retry
    /// </summary>
    [HttpGet("failed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedMessages(
        [FromQuery] DateTime? since = null)
    {
        var companyId = GetCompanyIdFromClaims();

        var failed = await _messageLogRepository.GetFailedMessagesAsync(
            companyId,
            since
        );

        return Ok(failed);
    }

    private Guid GetCompanyIdFromClaims()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value;
        return Guid.Parse(companyIdClaim ?? throw new UnauthorizedAccessException());
    }
}