using System;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Reembolso como a API devolve.
/// </summary>
public sealed class ExpenseResponse
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public required Guid Id { get; init; }

    /// <summary>Usuário dono do reembolso, obtido do token na criação.</summary>
    public required string OwnerId { get; init; }

    /// <summary>Categoria da despesa.</summary>
    public required int CategoryId { get; init; }

    /// <summary>Nome da categoria.</summary>
    public string? CategoryName { get; init; }

    /// <summary>Descrição da despesa.</summary>
    public required string Description { get; init; }

    /// <summary>Valor em reais.</summary>
    public required decimal Amount { get; init; }

    /// <summary>Data da despesa.</summary>
    public required DateOnly ExpenseDate { get; init; }

    /// <summary>Estado atual, definido só pelo servidor.</summary>
    public required ExpenseStatus Status { get; init; }

    /// <summary>Justificativa, preenchida quando o reembolso foi reprovado.</summary>
    public string? RejectionReason { get; init; }

    /// <summary>Criação, em UTC.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Última alteração, em UTC.</summary>
    public required DateTime UpdatedAt { get; init; }
}
