using LiaXP.Application.DTOs.Auth;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Auth;

public interface ILoginUseCase
{
    Task<Result<LoginResponse>> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public class LoginUseCase : ILoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<LoginUseCase> _logger;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ILogger<LoginUseCase> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.CompanyCode))
            {
                return Result<LoginResponse>.Failure("Email, senha e código da empresa são obrigatórios");
            }

            // Buscar usuário
            var user = await _userRepository.GetByEmailAsync(
                request.Email.ToLowerInvariant(),
                request.CompanyCode,
                cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Tentativa de login com credenciais inválidas: {Email}", request.Email);
                return Result<LoginResponse>.Failure("Credenciais inválidas");
            }

            // Verificar se está ativo
            if (!user.IsActive)
            {
                _logger.LogWarning("Tentativa de login com usuário inativo: {Email}", request.Email);
                return Result<LoginResponse>.Failure("Usuário inativo");
            }

            // Verificar senha
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Senha incorreta para usuário: {Email}", request.Email);
                return Result<LoginResponse>.Failure("Credenciais inválidas");
            }

            // Atualizar último login
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Gerar token
            var token = _tokenService.GenerateToken(user);

            var response = new LoginResponse
            {
                AccessToken = token,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                User = new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    CompanyCode = user.CompanyCode
                }
            };

            _logger.LogInformation("Login bem-sucedido para usuário: {Email}", request.Email);
            return Result<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar login para {Email}", request.Email);
            return Result<LoginResponse>.Failure("Erro ao processar login");
        }
    }
}

// Helper class para retorno de resultados
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Data { get; }
    public string? ErrorMessage { get; }

    private Result(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}