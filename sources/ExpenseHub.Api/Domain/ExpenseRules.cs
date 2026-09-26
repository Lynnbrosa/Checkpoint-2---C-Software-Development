using System;

namespace ExpenseHub.Api.Domain;

internal static class ExpenseRules
{
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 500;

    // texto porque o RangeAttribute com decimal só aceita os limites como string
    public const string MinAmountText = "0.01";
    public const string MaxAmountText = "2147483647";

    public const decimal MinAmount = 0.01m;
    public const decimal MaxAmount = int.MaxValue;

    public static bool IsValidDescription(string? description) =>
        !string.IsNullOrWhiteSpace(description)
        && description.Length >= DescriptionMinLength
        && description.Length <= DescriptionMaxLength;

    public static bool IsValidAmount(decimal amount) => amount >= MinAmount && amount <= MaxAmount;

    public static bool IsValidExpenseDate(DateOnly expenseDate, DateOnly today) => expenseDate <= today;
}
