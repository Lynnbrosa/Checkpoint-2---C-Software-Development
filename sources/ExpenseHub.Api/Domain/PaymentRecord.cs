using System;

namespace ExpenseHub.Api.Domain;

internal sealed class PaymentRecord
{
    private PaymentRecord()
    {
    }

    public long Id { get; private set; }

    public Guid ExpenseId { get; private set; }

    public decimal Amount { get; private set; }

    public string PaidById { get; private set; } = string.Empty;

    public DateTime PaidAt { get; private set; }
}
