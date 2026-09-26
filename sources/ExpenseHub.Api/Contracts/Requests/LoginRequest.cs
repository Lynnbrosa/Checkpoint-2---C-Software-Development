using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts.Requests;

/// <summary>
/// Credenciais para <c>POST /login</c>.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>E-mail cadastrado.</summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>Senha do usuário.</summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}
