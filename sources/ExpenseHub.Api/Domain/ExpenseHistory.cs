using System;

namespace ExpenseHub.Api.Domain;

internal sealed class ExpenseHistory
{
    public ExpenseHistory(
        Guid expenseId,
        ExpenseAction action,
        string actorId,
        DateTime occurredAt,
        ExpenseStatus? fromStatus,
        ExpenseStatus toStatus,
        string? justification = null,
        string? changes = null)
    {
        ExpenseId = expenseId;
        Action = action;
        ActorId = actorId;
        OccurredAt = occurredAt;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Justification = justification;
        Changes = changes;
    }

    private ExpenseHistory()
    {
    }

    public long Id { get; private set; }

    public Guid ExpenseId { get; private set; }

    public ExpenseAction Action { get; private set; }

    public string ActorId { get; private set; } = string.Empty;

    public DateTime OccurredAt { get; private set; }

    public ExpenseStatus? FromStatus { get; private set; }

    public ExpenseStatus ToStatus { get; private set; }

    public string? Justification { get; private set; }

    public string? Changes { get; private set; }
}
