using System;
using System.Collections.Generic;

namespace ExpenseHub.Api.Domain;

internal sealed class Expense
{
    private Expense()
    {
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    public int CategoryId { get; private set; }

    public ExpenseCategory? Category { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public decimal Amount { get; private set; }

    public DateOnly ExpenseDate { get; private set; }

    public ExpenseStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    public PaymentRecord? Payment { get; private set; }

    public ICollection<ExpenseHistory> History { get; } = new List<ExpenseHistory>();
}
