using System;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.UnitTests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 20, 15, 0, 0, TimeSpan.Zero);

    public static readonly DateOnly Today = new(2026, 9, 20);

    public static ExpenseDraftRequest DraftRequest(
        string description = "Táxi do aeroporto até o cliente",
        decimal amount = 87.50m,
        DateOnly? expenseDate = null,
        int categoryId = 1) => new()
        {
            Description = description,
            Amount = amount,
            ExpenseDate = expenseDate ?? Today.AddDays(-3),
            CategoryId = categoryId,
        };

    public static Expense Draft(UserContext owner) => Expense.CreateDraft(
        owner.Id,
        new ExpenseCategory(1, "Transporte"),
        "Almoço com cliente em São Paulo",
        120.00m,
        Today.AddDays(-5),
        Now.UtcDateTime.AddDays(-1));

    public static Expense Submitted(UserContext owner)
    {
        Expense expense = Draft(owner);
        expense.Submit(owner.Id, Now.UtcDateTime.AddHours(-12));
        return expense;
    }

    public static Expense Approved(UserContext owner)
    {
        Expense expense = Submitted(owner);
        expense.Approve("approver-seed", Now.UtcDateTime.AddHours(-6));
        return expense;
    }

    public static Expense Paid(UserContext owner)
    {
        Expense expense = Approved(owner);
        expense.Pay("finance-seed", Now.UtcDateTime.AddHours(-1));
        return expense;
    }

    public static Expense Rejected(UserContext owner)
    {
        Expense expense = Submitted(owner);
        expense.Reject("approver-seed", "Faltou o comprovante da despesa.", Now.UtcDateTime.AddHours(-6));
        return expense;
    }
}
