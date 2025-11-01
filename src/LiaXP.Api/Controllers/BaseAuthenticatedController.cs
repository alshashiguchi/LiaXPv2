using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LiaXP.Api.Controllers;

/// <summary>
/// Base controller with common functionality for authenticated endpoints
/// Provides helper methods to extract user claims from JWT token
/// </summary>
public abstract class BaseAuthenticatedController : ControllerBase
{
    /// <summary>
    /// Get the authenticated user's ID from JWT claims
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">If user ID claim is missing or invalid</exception>
    protected Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("user_id")?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format in token");
        }

        return userId;
    }

    /// <summary>
    /// Get the authenticated user's CompanyId (GUID) from JWT claims
    /// IMPORTANT: JWT should contain CompanyId (GUID), not CompanyCode (string)
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">If company ID claim is missing or invalid</exception>
    protected Guid GetCompanyId()
    {
        var companyIdClaim = User.FindFirst("company_id")?.Value
            ?? User.FindFirst("companyId")?.Value
            ?? User.FindFirst("CompanyId")?.Value;

        if (string.IsNullOrEmpty(companyIdClaim))
        {
            throw new UnauthorizedAccessException("Company ID not found in token");
        }

        if (!Guid.TryParse(companyIdClaim, out var companyId))
        {
            throw new UnauthorizedAccessException("Invalid company ID format in token");
        }

        return companyId;
    }

    /// <summary>
    /// Get the authenticated user's email from JWT claims
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">If email claim is missing</exception>
    protected string GetUserEmail()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;

        if (string.IsNullOrEmpty(email))
        {
            throw new UnauthorizedAccessException("Email not found in token");
        }

        return email;
    }

    /// <summary>
    /// Get the authenticated user's role from JWT claims
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">If role claim is missing</exception>
    protected string GetUserRole()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        if (string.IsNullOrEmpty(role))
        {
            throw new UnauthorizedAccessException("Role not found in token");
        }

        return role;
    }

    /// <summary>
    /// Check if the authenticated user has a specific role
    /// </summary>
    protected bool IsInRole(string role)
    {
        return User.IsInRole(role);
    }

    /// <summary>
    /// Check if the authenticated user is an admin
    /// </summary>
    protected bool IsAdmin()
    {
        return IsInRole("Admin");
    }

    /// <summary>
    /// Check if the authenticated user is a manager or admin
    /// </summary>
    protected bool IsManagerOrAdmin()
    {
        return IsInRole("Admin") || IsInRole("Manager");
    }

    /// <summary>
    /// Validate that the user has access to the specified company
    /// </summary>
    /// <param name="companyId">Company ID to validate</param>
    /// <exception cref="UnauthorizedAccessException">If user doesn't belong to the company</exception>
    protected void ValidateCompanyAccess(Guid companyId)
    {
        var userCompanyId = GetCompanyId();

        if (userCompanyId != companyId)
        {
            throw new UnauthorizedAccessException(
                $"User does not have access to company {companyId}");
        }
    }
}