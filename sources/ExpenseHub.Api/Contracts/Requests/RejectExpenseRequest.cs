using System.ComponentModel.DataAnnotations;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts.Requests;

/// <summary>
/// Corpo de <c>POST /api/expenses/{id}/reject</c>.
/// </summary>
public sealed class RejectExpenseRequest
{
    /// <summary>Motivo da reprovação, entre 10 e 500 caracteres.</summary>
    [Required]
    [StringLength(ExpenseRules.JustificationMaxLength, MinimumLength = ExpenseRules.JustificationMinLength)]
    public required string Justification { get; init; }
}
