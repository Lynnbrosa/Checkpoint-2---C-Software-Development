using System.Collections.Generic;

namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Dados do usuário autenticado, lidos do token.
/// </summary>
public sealed class CurrentUserResponse
{
    /// <summary>Identificador do usuário.</summary>
    public required string Id { get; init; }

    /// <summary>E-mail do usuário.</summary>
    public string? Email { get; init; }

    /// <summary>Roles presentes no token atual.</summary>
    public required IReadOnlyList<string> Roles { get; init; }
}
