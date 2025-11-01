using System.ComponentModel.DataAnnotations;

namespace LiaXP.Application.DTOs.Auth;

/// <summary>
/// Login request DTO
/// User provides: email, password, and company code (business identifier)
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email inválido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (plain text - will be hashed)
    /// </summary>
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Company business code (e.g., "ACME", "CONTOSO")
    /// This will be converted to CompanyId (GUID) internally
    /// </summary>
    [Required(ErrorMessage = "Código da empresa é obrigatório")]
    public string CompanyCode { get; set; } = string.Empty;
}
