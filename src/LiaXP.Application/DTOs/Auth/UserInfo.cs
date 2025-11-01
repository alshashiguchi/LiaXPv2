using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiaXP.Application.DTOs.Auth;
/// <summary>
/// User information included in login response
/// Contains both CompanyId (technical) and CompanyCode (display)
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Company technical identifier (GUID)
    /// Used in JWT claims and internal operations
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Company business code (e.g., "ACME")
    /// User-friendly identifier for display
    /// </summary>
    public string CompanyCode { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User role (Admin, Manager, Seller)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// User active status
    /// </summary>
    public bool IsActive { get; set; }
}