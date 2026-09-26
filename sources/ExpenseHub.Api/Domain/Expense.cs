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

    public static Expense CreateDraft(
        string ownerId,
        ExpenseCategory category,
        string description,
        decimal amount,
        DateOnly expenseDate,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(category);
        EnsureValidDraft(description, amount);

        Expense expense = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            CategoryId = category.Id,
            Category = category,
            Description = description,
            Amount = amount,
            ExpenseDate = expenseDate,
            Status = ExpenseStatus.Draft,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc,
            ConcurrencyStamp = Guid.NewGuid(),
        };

        expense.History.Add(new ExpenseHistory(
            expense.Id,
            ExpenseAction.Created,
            ownerId,
            nowUtc,
            fromStatus: null,
            toStatus: ExpenseStatus.Draft));

        return expense;
    }

    private static void EnsureValidDraft(string description, decimal amount)
    {
        if (!ExpenseRules.IsValidDescription(description))
        {
            throw new ArgumentException("Descrição fora do limite de 10 a 500 caracteres.", nameof(description));
        }

        if (!ExpenseRules.IsValidAmount(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Valor fora do limite permitido.");
        }
    }
}
