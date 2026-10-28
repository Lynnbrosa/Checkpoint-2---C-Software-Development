using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpenseHub.Api.Domain;

/// <summary>
/// Resumo calculado por estado de reembolso.
/// </summary>
/// <param name="Status">Estado do reembolso.</param>
/// <param name="Count">Quantidade de reembolsos no estado.</param>
/// <param name="Amount">Soma dos valores no estado.</param>
internal sealed record ReportStatusSummary(ExpenseStatus Status, int Count, decimal Amount);

/// <summary>
/// Resultado consolidado de um relatório de reembolsos por período.
/// </summary>
/// <param name="GeneratedAt">Instante da conclusão do cálculo em UTC.</param>
/// <param name="TotalCount">Total de reembolsos considerados.</param>
/// <param name="TotalAmount">Soma total dos valores considerados.</param>
/// <param name="ByStatus">Totais discriminados pelos cinco estados de reembolso.</param>
internal sealed record ReportSummary(
    DateTime GeneratedAt,
    int TotalCount,
    decimal TotalAmount,
    IReadOnlyList<ReportStatusSummary> ByStatus)
{
    private static readonly ExpenseStatus[] _orderedStatuses =
    [
        ExpenseStatus.Draft,
        ExpenseStatus.Submitted,
        ExpenseStatus.Approved,
        ExpenseStatus.Rejected,
        ExpenseStatus.Paid,
    ];

    public static ReportSummary Calculate(IEnumerable<Expense> expenses, DateTime generatedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        Dictionary<ExpenseStatus, (int Count, decimal Amount)> totals = _orderedStatuses
            .ToDictionary(status => status, _ => (Count: 0, Amount: 0.00m));

        int totalCount = 0;
        decimal totalAmount = 0.00m;

        foreach (Expense expense in expenses)
        {
            (int count, decimal amount) = totals[expense.Status];
            totals[expense.Status] = (count + 1, amount + expense.Amount);
            totalCount++;
            totalAmount += expense.Amount;
        }

        List<ReportStatusSummary> byStatus = _orderedStatuses
            .Select(status =>
            {
                (int count, decimal amount) = totals[status];
                return new ReportStatusSummary(status, count, amount);
            })
            .ToList();

        return new ReportSummary(generatedAtUtc, totalCount, totalAmount, byStatus);
    }
}

