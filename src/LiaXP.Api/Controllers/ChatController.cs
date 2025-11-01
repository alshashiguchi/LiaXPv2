using LiaXP.Application.DTOs.Chat;
using LiaXP.Application.UseCases.Chat;
using LiaXP.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

/// <summary>
/// Chat controller for AI assistant interactions
/// FIXED: Now uses CompanyId (GUID) from JWT instead of CompanyCode
/// </summary>
[Authorize]
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : BaseAuthenticatedController
{
    private readonly IProcessChatMessageUseCase _processChatUseCase;
    private readonly ICompanyResolver _companyResolver;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IProcessChatMessageUseCase processChatUseCase,
        ICompanyResolver companyResolver,
        ILogger<ChatController> logger)
    {
        _processChatUseCase = processChatUseCase;
        _companyResolver = companyResolver;
        _logger = logger;
    }

    /// <summary>
    /// Process a chat message with the AI assistant
    /// </summary>
    /// <param name="request">Chat message request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI assistant response</returns>
    [HttpPost("message")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SimulateChat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida",
                Detail = "A mensagem não pode estar vazia"
            });
        }

        try
        {
            // ✅ FIXED: Get CompanyId (GUID) from JWT token
            var userId = GetUserId();
            var companyId = GetCompanyId();

            _logger.LogInformation(
                "Processing chat message | UserId: {UserId} | CompanyId: {CompanyId}",
                userId,
                companyId);

            // ✅ OPTIONAL: Get CompanyCode for logging/display
            var companyCode = await _companyResolver.GetCompanyCodeAsync(
                companyId,
                cancellationToken);

            _logger.LogDebug(
                "Resolved CompanyCode | CompanyId: {CompanyId} | CompanyCode: {CompanyCode}",
                companyId,
                companyCode);

            // Execute use case with CompanyId (GUID)
            var result = await _processChatUseCase.ExecuteAsync(
                request,
                userId,
                companyId,  // ✅ Pass CompanyId (GUID) not CompanyCode
                cancellationToken);

            if (!result.IsSuccess)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Erro ao processar chat",
                    Detail = result.ErrorMessage
                });
            }

            return Ok(result.Data);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(
                ex,
                "Unauthorized access attempt | Message: {Message}",
                ex.Message);

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não autorizado",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing chat message | UserId: {UserId}",
                GetUserId());

            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno",
                Detail = "Ocorreu um erro ao processar sua mensagem"
            });
        }
    }

    /// <summary>
    /// Get chat history for the authenticated user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(List<ChatHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChatHistory(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = GetUserId();
            var companyId = GetCompanyId();

            var history = await _processChatUseCase.GetHistoryAsync(
                userId,
                companyId,
                limit,
                cancellationToken);

            return Ok(history);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to chat history");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não autorizado",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat history");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
