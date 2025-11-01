using LiaXP.Application.DTOs.Auth;
using LiaXP.Application.DTOs.Common;
using LiaXP.Domain.Entities;
using LiaXP.Domain.Interfaces;
using LiaXP.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

/// <summary>
/// User management controller
/// Demonstrates how to accept both CompanyId and CompanyCode in API requests
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyResolver _companyResolver;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserRepository userRepository,
        ICompanyResolver companyResolver,
        IPasswordHasher passwordHasher,
        ILogger<UserController> logger)
    {
        _userRepository = userRepository;
        _companyResolver = companyResolver;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    /// <summary>
    /// Create a new user
    /// Accepts either CompanyId (GUID) or CompanyCode (string)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // ✅ Step 1: Resolve CompanyId from either CompanyId or CompanyCode
            var companyId = await ResolveCompanyIdAsync(
                request.CompanyId,
                request.CompanyCode,
                cancellationToken);

            if (companyId == null)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "InvalidCompany",
                    Message = "Company not found. Provide a valid CompanyId or CompanyCode."
                });
            }

            // ✅ Step 2: Validate company is active
            var isActive = await _companyResolver.ValidateCompanyIdAsync(
                companyId.Value,
                cancellationToken);

            if (!isActive)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "InactiveCompany",
                    Message = "Company is not active."
                });
            }

            // ✅ Step 3: Hash password
            var passwordHash = _passwordHasher.HashPassword(request.Password);

            // ✅ Step 4: Create user entity (using CompanyId)
            var user = new User(
                companyId: companyId.Value,
                email: request.Email,
                passwordHash: passwordHash,
                fullName: request.FullName,
                role: request.Role
            );

            // ✅ Step 5: Save to database
            var createdUser = await _userRepository.AddAsync(user, cancellationToken);

            // ✅ Step 6: Get CompanyCode for response (optional - for user-friendly display)
            var companyCode = await _companyResolver.GetCompanyCodeAsync(
                companyId.Value,
                cancellationToken);

            _logger.LogInformation(
                "User created | UserId: {UserId} | Email: {Email} | CompanyId: {CompanyId} | CompanyCode: {CompanyCode}",
                createdUser.Id,
                createdUser.Email,
                companyId.Value,
                companyCode);

            // ✅ Step 7: Return response with both CompanyId and CompanyCode
            var response = new UserResponse
            {
                Id = createdUser.Id,
                CompanyId = createdUser.CompanyId,
                CompanyCode = companyCode,  // User-friendly display
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                Role = createdUser.Role.ToString(),
                IsActive = createdUser.IsActive,
                CreatedAt = createdUser.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetUserById),
                new { id = response.Id },
                response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create user | Email: {Email}", request.Email);
            return BadRequest(new ErrorResponse
            {
                Error = "UserCreationFailed",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user | Email: {Email}", request.Email);
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalServerError",
                Message = "An unexpected error occurred."
            });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        // Get CompanyCode for display
        var companyCode = await _companyResolver.GetCompanyCodeAsync(
            user.CompanyId,
            cancellationToken);

        var response = new UserResponse
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            CompanyCode = companyCode,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(response);
    }

    /// <summary>
    /// List users by company
    /// Accepts either CompanyId or CompanyCode as query parameter
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(List<UserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListUsers(
        [FromQuery] Guid? companyId,
        [FromQuery] string? companyCode,
        CancellationToken cancellationToken)
    {
        // ✅ Resolve CompanyId
        var resolvedCompanyId = await ResolveCompanyIdAsync(
            companyId,
            companyCode,
            cancellationToken);

        if (resolvedCompanyId == null)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "InvalidCompany",
                Message = "Provide a valid CompanyId or CompanyCode."
            });
        }

        // Get users for company
        var users = await _userRepository.GetByCompanyIdAsync(
            resolvedCompanyId.Value,
            cancellationToken);

        // Get CompanyCode once for all users
        var resolvedCompanyCode = await _companyResolver.GetCompanyCodeAsync(
            resolvedCompanyId.Value,
            cancellationToken);

        var response = users.Select(u => new UserResponse
        {
            Id = u.Id,
            CompanyId = u.CompanyId,
            CompanyCode = resolvedCompanyCode,
            Email = u.Email,
            FullName = u.FullName,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Update user
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound();
        }

        // Update user profile
        user.UpdateProfile(request.FullName, request.Email);

        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get CompanyCode for response
        var companyCode = await _companyResolver.GetCompanyCodeAsync(
            user.CompanyId,
            cancellationToken);

        var response = new UserResponse
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            CompanyCode = companyCode,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Helper method to resolve CompanyId from either CompanyId or CompanyCode
    /// Returns null if neither is valid
    /// </summary>
    private async Task<Guid?> ResolveCompanyIdAsync(
        Guid? companyId,
        string? companyCode,
        CancellationToken cancellationToken)
    {
        // Priority 1: Use CompanyId if provided and valid
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            var exists = await _companyResolver.ValidateCompanyIdAsync(
                companyId.Value,
                cancellationToken);

            return exists ? companyId.Value : null;
        }

        // Priority 2: Resolve from CompanyCode if provided
        if (!string.IsNullOrWhiteSpace(companyCode))
        {
            return await _companyResolver.GetCompanyIdAsync(companyCode, cancellationToken);
        }

        // Neither provided or valid
        return null;
    }
}
