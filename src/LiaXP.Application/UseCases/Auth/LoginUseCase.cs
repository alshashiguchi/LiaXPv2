using LiaXP.Application.DTOs.Auth;
using LiaXP.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LiaXP.Application.UseCases.Auth;

/// <summary>
/// Use case for user login/authentication
/// Handles CompanyCode (string) to CompanyId (GUID) conversion
/// </summary>
public interface ILoginUseCase
{
    Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}

public class LoginUseCase : ILoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyResolver _companyResolver;
    private readonly ITokenService _jwtService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<LoginUseCase> _logger;

    public LoginUseCase(
        IUserRepository userRepository,
        ICompanyResolver companyResolver,
        ITokenService jwtService,
        IPasswordHasher passwordHasher,
        ILogger<LoginUseCase> logger)
    {
        _userRepository = userRepository;
        _companyResolver = companyResolver;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Login attempt | Email: {Email} | CompanyCode: {CompanyCode}",
                request.Email,
                request.CompanyCode);

            // ✅ Step 1: Resolve CompanyCode (string) to CompanyId (GUID)
            var companyId = await _companyResolver.GetCompanyIdAsync(
                request.CompanyCode,
                cancellationToken);

            if (companyId == null)
            {
                _logger.LogWarning(
                    "Company not found | CompanyCode: {CompanyCode}",
                    request.CompanyCode);

                return Result<LoginResponse>.Failure(
                    "Empresa não encontrada. Verifique o código da empresa.");
            }

            _logger.LogDebug(
                "Company resolved | CompanyCode: {CompanyCode} | CompanyId: {CompanyId}",
                request.CompanyCode,
                companyId);

            // ✅ Step 2: Get user by email and companyId (GUID)
            var user = await _userRepository.GetByEmailAndCompanyAsync(
                request.Email.ToLowerInvariant(),
                companyId.Value,
                cancellationToken);

            if (user == null)
            {
                _logger.LogWarning(
                    "User not found | Email: {Email} | CompanyId: {CompanyId}",
                    request.Email,
                    companyId);

                return Result<LoginResponse>.Failure(
                    "Email ou senha incorretos.");
            }

            // ✅ Step 3: Validate user is active
            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "Inactive user login attempt | UserId: {UserId}",
                    user.Id);

                return Result<LoginResponse>.Failure(
                    "Usuário inativo. Entre em contato com o administrador.");
            }

            // ✅ Step 4: Verify password
            if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning(
                    "Invalid password | UserId: {UserId}",
                    user.Id);

                return Result<LoginResponse>.Failure(
                    "Email ou senha incorretos.");
            }

            // ✅ Step 5: Update last login timestamp
            user.UpdateLastLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            // ✅ Step 6: Generate JWT token (with CompanyId GUID in claims)
            // CompanyCode is passed for optional display in token
            var token = _jwtService.GenerateToken(user, request.CompanyCode);

            _logger.LogInformation(
                "Login successful | UserId: {UserId} | CompanyId: {CompanyId} | Role: {Role}",
                user.Id,
                user.CompanyId,
                user.Role);

            // ✅ Step 7: Build response with both CompanyId and CompanyCode
            var response = new LoginResponse
            {
                Token = token,
                User = new UserInfo
                {
                    Id = user.Id,
                    CompanyId = user.CompanyId,           // ✅ Technical ID
                    CompanyCode = request.CompanyCode,    // ✅ Business code (display)
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    IsActive = user.IsActive
                }
            };

            return Result<LoginResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during login | Email: {Email} | CompanyCode: {CompanyCode}",
                request.Email,
                request.CompanyCode);

            return Result<LoginResponse>.Failure(
                "Erro ao processar login. Tente novamente.");
        }
    }
}

// ============================================================================
// Result Helper
// ============================================================================

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Result(bool isSuccess, T? data, string? errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public static Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}