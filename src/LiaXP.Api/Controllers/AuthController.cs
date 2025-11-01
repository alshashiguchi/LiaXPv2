using LiaXP.Application.DTOs.Auth;
using LiaXP.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

/// <summary>
/// Authentication controller
/// Handles user login and authentication
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly ILoginUseCase _loginUseCase;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ILoginUseCase loginUseCase,
        ILogger<AuthController> logger)
    {
        _loginUseCase = loginUseCase;
        _logger = logger;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login credentials (email, password, companyCode)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JWT token and user information</returns>
    /// <response code="200">Login successful - returns JWT token</response>
    /// <response code="400">Invalid credentials or validation errors</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        // Validate model
        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida",
                Detail = "Verifique os campos fornecidos",
                Extensions = { ["errors"] = ModelState }
            });
        }

        try
        {
            _logger.LogInformation(
                "Login request received | Email: {Email} | CompanyCode: {CompanyCode}",
                request.Email,
                request.CompanyCode);

            // Execute login use case
            var result = await _loginUseCase.ExecuteAsync(request, cancellationToken);

            if (!result.IsSuccess || result.Data == null)
            {
                _logger.LogWarning(
                    "Login failed | Email: {Email} | CompanyCode: {CompanyCode} | Reason: {Reason}",
                    request.Email,
                    request.CompanyCode,
                    result.ErrorMessage);

                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Falha no login",
                    Detail = result.ErrorMessage
                });
            }

            _logger.LogInformation(
                "Login successful | UserId: {UserId} | Email: {Email}",
                result.Data.User.Id,
                result.Data.User.Email);

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing login | Email: {Email} | CompanyCode: {CompanyCode}",
                request.Email,
                request.CompanyCode);

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno",
                    Detail = "Ocorreu um erro ao processar o login. Tente novamente."
                });
        }
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        try
        {
            // Extract user info from JWT claims
            var userId = User.FindFirst("user_id")?.Value;
            var companyId = User.FindFirst("company_id")?.Value;
            var companyCode = User.FindFirst("company_code")?.Value;
            var email = User.FindFirst("email")?.Value;
            var fullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(companyId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Token inválido",
                    Detail = "Token de autenticação inválido ou expirado"
                });
            }

            var userInfo = new UserInfo
            {
                Id = Guid.Parse(userId),
                CompanyId = Guid.Parse(companyId),
                CompanyCode = companyCode ?? "",
                Email = email ?? "",
                FullName = fullName ?? "",
                Role = role ?? "",
                IsActive = true
            };

            return Ok(userInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user information");

            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Erro de autenticação",
                Detail = "Não foi possível validar o token de autenticação"
            });
        }
    }

    /// <summary>
    /// Logout endpoint (optional - JWT is stateless)
    /// Client should discard the token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        _logger.LogInformation(
            "Logout request | UserId: {UserId}",
            User.FindFirst("user_id")?.Value);

        return Ok(new { message = "Logout realizado com sucesso" });
    }
}