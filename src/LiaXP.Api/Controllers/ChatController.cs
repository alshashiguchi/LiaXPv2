using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiaXP.Application.UseCases;
using LiaXP.Application.DTOs.Chat;
using System.Security.Claims;

namespace LiaXP.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("admin/chat-simulator")]
public class ChatController : ControllerBase
{
    private readonly ProcessChatMessageUseCase _processChatUseCase;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        ProcessChatMessageUseCase processChatUseCase,
        ILogger<ChatController> logger)
    {
        _processChatUseCase = processChatUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Simula conversa WhatsApp para testes
    /// </summary>
    /// <param name="request">Mensagem do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resposta da IA</returns>
    /// <response code="200">Mensagem processada com sucesso</response>
    /// <response code="400">Requisição inválida</response>
    /// <response code="401">Não autenticado</response>
    [HttpPost]
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
            var userId = GetUserId();
            var companyCode = GetCompanyCode();

            var result = await _processChatUseCase.ExecuteAsync(
                request,
                userId,
                companyCode,
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
            _logger.LogWarning(ex, "Unauthorized access attempt");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Não autorizado",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Erro interno",
                Detail = "Ocorreu um erro ao processar sua mensagem"
            });
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException("User ID não encontrado"));
    }

    private string GetCompanyCode()
    {
        return User.FindFirst("company_code")?.Value
            ?? throw new UnauthorizedAccessException("Company code não encontrado");
    }
}