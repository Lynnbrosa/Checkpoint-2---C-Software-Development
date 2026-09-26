using System;
using System.ComponentModel.DataAnnotations;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts.Requests;

/// <summary>
/// Campos editáveis de um rascunho, usados na criação e na edição.
/// Proprietário, estado, atores e horários não fazem parte do contrato.
/// </summary>
public sealed class ExpenseDraftRequest
{
    /// <summary>Descrição da despesa, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(ExpenseRules.DescriptionMaxLength, MinimumLength = ExpenseRules.DescriptionMinLength)]
    public required string Description { get; init; }

    /// <summary>Valor em reais, de 0,01 a 2.147.483.647,00.</summary>
    [Range(
        typeof(decimal),
        ExpenseRules.MinAmountText,
        ExpenseRules.MaxAmountText,
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public required decimal Amount { get; init; }

    /// <summary>Data da despesa no formato <c>yyyy-MM-dd</c>. Não pode ser futura.</summary>
    public required DateOnly ExpenseDate { get; init; }

    /// <summary>Categoria da despesa (1 a 5, ver README).</summary>
    [Range(1, int.MaxValue)]
    public required int CategoryId { get; init; }
}
