using LiaXP.Application.DTOs.Auth;
using LiaXP.Application.UseCases.Auth;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILoginUseCase _loginUseCase;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILoginUseCase loginUseCase, ILogger<AuthController> logger)
    {
        _loginUseCase = loginUseCase;
        _logger = logger;
    }

    /// <summary>
    /// Autentica um usuário e retorna um token JWT
    /// </summary>
    /// <param name="request">Credenciais de login</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Token de acesso e informações do usuário</returns>
    /// <response code="200">Login realizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Credenciais inválidas</response>
    [HttpPost("token")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _loginUseCase.ExecuteAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Autenticação falhou",
                Detail = result.ErrorMessage
            });
        }

        return Ok(result.Data);
    }
}