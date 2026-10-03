using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts.Requests;

/// <summary>
/// Cadastro de usuário em <c>POST /register</c>. Não existe campo de role: quem dá role é o Admin.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>E-mail, usado também como login.</summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    /// <summary>Senha, validada pela política do Identity.</summary>
    [Required]
    public required string Password { get; init; }

    /// <summary>Nome para exibição (opcional).</summary>
    [StringLength(120)]
    public string? FullName { get; init; }
}
