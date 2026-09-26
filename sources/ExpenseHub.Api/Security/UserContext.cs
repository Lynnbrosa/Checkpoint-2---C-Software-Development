using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ExpenseHub.Api.Security;

/// <summary>
/// Identidade autenticada que chega às regras de negócio. Sempre montada a partir do token.
/// </summary>
public sealed class UserContext
{
    /// <summary>Cria o contexto com o identificador e as roles do usuário.</summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="roles">Roles presentes no token.</param>
    public UserContext(string id, IEnumerable<string> roles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(roles);

        Id = id;
        Roles = roles.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Identificador do usuário autenticado.</summary>
    public string Id { get; }

    /// <summary>Roles do usuário no momento do login.</summary>
    public IReadOnlySet<string> Roles { get; }

    /// <summary>Indica se o usuário possui a role informada.</summary>
    /// <param name="role">Nome da role.</param>
    /// <returns><see langword="true"/> quando a role está presente.</returns>
    public bool IsInRole(string role) => Roles.Contains(role);

    /// <summary>Monta o contexto a partir das claims do token bearer.</summary>
    /// <param name="principal">Usuário autenticado da requisição.</param>
    /// <returns>Contexto com id e roles.</returns>
    public static UserContext FromPrincipal(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        string id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Token sem identificador de usuário.");
        IEnumerable<string> roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

        return new UserContext(id, roles);
    }
}
