using System;
using System.Collections.Generic;
using System.Globalization;

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

    public bool CanApply(ExpenseAction action) => ExpenseWorkflow.TryGetNextStatus(Status, action, out _);

    public IReadOnlyList<ExpenseFieldChange> UpdateDraft(
        ExpenseCategory category,
        string description,
        decimal amount,
        DateOnly expenseDate,
        string actorId,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(category);
        EnsureValidDraft(description, amount);
        ExpenseStatus next = NextStatusFor(ExpenseAction.Updated);

        List<ExpenseFieldChange> changes = [];
        if (!string.Equals(Description, description, StringComparison.Ordinal))
        {
            changes.Add(new ExpenseFieldChange("description", Description, description));
        }

        if (Amount != amount)
        {
            changes.Add(new ExpenseFieldChange("amount", FormatAmount(Amount), FormatAmount(amount)));
        }

        if (ExpenseDate != expenseDate)
        {
            changes.Add(new ExpenseFieldChange("expenseDate", FormatDate(ExpenseDate), FormatDate(expenseDate)));
        }

        if (CategoryId != category.Id)
        {
            changes.Add(new ExpenseFieldChange("categoryId", FormatId(CategoryId), FormatId(category.Id)));
        }

        // PUT igual ao que já está salvo não é alteração, então não entra no histórico
        if (changes.Count == 0)
        {
            return changes;
        }

        Description = description;
        Amount = amount;
        ExpenseDate = expenseDate;
        CategoryId = category.Id;
        Category = category;
        Record(ExpenseAction.Updated, actorId, nowUtc, next, changes: ExpenseChangeLog.Serialize(changes));

        return changes;
    }

    public void Submit(string actorId, DateTime nowUtc)
    {
        Record(ExpenseAction.Submitted, actorId, nowUtc, NextStatusFor(ExpenseAction.Submitted));
    }

    public void Approve(string approverId, DateTime nowUtc)
    {
        Record(ExpenseAction.Approved, approverId, nowUtc, NextStatusFor(ExpenseAction.Approved));
    }

    public void Reject(string approverId, string justification, DateTime nowUtc)
    {
        if (!ExpenseRules.IsValidJustification(justification))
        {
            throw new ArgumentException("Justificativa fora do limite de 10 a 500 caracteres.", nameof(justification));
        }

        ExpenseStatus next = NextStatusFor(ExpenseAction.Rejected);
        RejectionReason = justification;
        Record(ExpenseAction.Rejected, approverId, nowUtc, next, justification: justification);
    }

    // pagamento simulado: registra quem pagou, quando e o valor aprovado, sem gateway
    public void Pay(string financeId, DateTime nowUtc)
    {
        ExpenseStatus next = NextStatusFor(ExpenseAction.Paid);
        Payment = new PaymentRecord(Id, Amount, financeId, nowUtc);
        Record(ExpenseAction.Paid, financeId, nowUtc, next);
    }

    private ExpenseStatus NextStatusFor(ExpenseAction action)
    {
        if (!ExpenseWorkflow.TryGetNextStatus(Status, action, out ExpenseStatus next))
        {
            throw new InvalidOperationException($"A ação {action} não é permitida em {Status}.");
        }

        return next;
    }

    // toda mudança passa por aqui: estado e histórico mudam juntos e vão no mesmo SaveChanges
    private void Record(
        ExpenseAction action,
        string actorId,
        DateTime nowUtc,
        ExpenseStatus next,
        string? justification = null,
        string? changes = null)
    {
        ExpenseStatus previous = Status;
        Status = next;
        UpdatedAt = nowUtc;
        ConcurrencyStamp = Guid.NewGuid();
        History.Add(new ExpenseHistory(Id, action, actorId, nowUtc, previous, next, justification, changes));
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

    private static string FormatAmount(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatId(int value) => value.ToString(CultureInfo.InvariantCulture);
}
