namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Campo alterado numa edição de rascunho.
/// </summary>
public sealed class FieldChangeResponse
{
    /// <summary>Nome do campo no contrato (description, amount, expenseDate, categoryId).</summary>
    public required string Field { get; init; }

    /// <summary>Valor anterior.</summary>
    public required string From { get; init; }

    /// <summary>Valor novo.</summary>
    public required string To { get; init; }
}
