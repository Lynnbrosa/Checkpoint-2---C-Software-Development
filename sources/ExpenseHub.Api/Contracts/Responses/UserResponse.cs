using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Usuário como a API devolve. Nunca inclui senha ou hash.
/// </summary>
public sealed class UserResponse
{
    /// <summary>Identificador do usuário.</summary>
    public required string Id { get; init; }

    /// <summary>E-mail do usuário.</summary>
    public required string Email { get; init; }

    /// <summary>Nome para exibição.</summary>
    public required string FullName { get; init; }

    /// <summary>Roles atuais, em ordem alfabética.</summary>
    public required IReadOnlyList<string> Roles { get; init; }
}
