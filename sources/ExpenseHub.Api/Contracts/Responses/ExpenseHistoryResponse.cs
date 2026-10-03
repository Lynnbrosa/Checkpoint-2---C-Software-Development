using System;
using System.Collections.Generic;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Uma entrada do histórico de um reembolso.
/// </summary>
public sealed class ExpenseHistoryResponse
{
    /// <summary>Ação registrada.</summary>
    public required ExpenseAction Action { get; init; }

    /// <summary>Usuário que executou a ação.</summary>
    public required string ActorId { get; init; }

    /// <summary>Momento da ação, em UTC.</summary>
    public required DateTime OccurredAt { get; init; }

    /// <summary>Estado antes da ação; vazio na criação.</summary>
    public ExpenseStatus? FromStatus { get; init; }

    /// <summary>Estado depois da ação.</summary>
    public required ExpenseStatus ToStatus { get; init; }

    /// <summary>Justificativa, na reprovação.</summary>
    public string? Justification { get; init; }

    /// <summary>Campos alterados, nas edições de rascunho.</summary>
    public required IReadOnlyList<FieldChangeResponse> Changes { get; init; }
}
