using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ExpenseHub.Api.Contracts.Requests;

/// <summary>
/// Conjunto completo de roles que o usuário deve ter depois da alteração.
/// Role que não estiver na lista é removida; lista vazia remove todas.
/// </summary>
public sealed class UpdateUserRolesRequest
{
    /// <summary>Roles desejadas: Admin, Employee, Approver, Finance ou Auditor.</summary>
    [Required]
    public required IReadOnlyList<string> Roles { get; init; }
}
