using System;
using System.Collections.Generic;
using System.Linq;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ReportSummaryTests
{
    private static readonly DateTime _generatedAt = new(2026, 10, 1, 12, 0, 2, DateTimeKind.Utc);

    [TestMethod]
    public void Calculate_EmptySet_ReturnsZeroTotalsAndAllFiveStatuses()
    {
        ReportSummary summary = ReportSummary.Calculate([], _generatedAt);

        Assert.AreEqual(_generatedAt, summary.GeneratedAt);
        Assert.AreEqual(0, summary.TotalCount);
        Assert.AreEqual(0.00m, summary.TotalAmount);
        Assert.HasCount(5, summary.ByStatus);

        CollectionAssert.AreEqual(
            new[]
            {
                ExpenseStatus.Draft,
                ExpenseStatus.Submitted,
                ExpenseStatus.Approved,
                ExpenseStatus.Rejected,
                ExpenseStatus.Paid,
            },
            summary.ByStatus.Select(item => item.Status).ToArray());

        Assert.IsTrue(summary.ByStatus.All(item => item.Count == 0 && item.Amount == 0.00m));
    }

    [TestMethod]
    public void Calculate_KnownDataset_ReturnsExactDecimalSumsByStatus()
    {
        ExpenseCategory category = new(1, "Transporte");
        DateOnly inPeriod = new(2026, 9, 15);
        DateTime now = _generatedAt.AddHours(-2);

        Expense approved1 = Expense.CreateDraft("emp-1", category, "Táxi corporativo centro", 100.00m, inPeriod, now);
        approved1.Submit("emp-1", now);
        approved1.Approve("approver-1", now);

        Expense approved2 = Expense.CreateDraft("emp-1", category, "Almoço reunião externa", 50.00m, inPeriod, now);
        approved2.Submit("emp-1", now);
        approved2.Approve("approver-1", now);

        Expense paid = Expense.CreateDraft("emp-1", category, "Estacionamento visita", 25.00m, inPeriod, now);
        paid.Submit("emp-1", now);
        paid.Approve("approver-1", now);
        paid.Pay("finance-1", now);

        List<Expense> expenses = [approved1, approved2, paid];

        ReportSummary summary = ReportSummary.Calculate(expenses, _generatedAt);

        Assert.AreEqual(3, summary.TotalCount);
        Assert.AreEqual(175.00m, summary.TotalAmount);

        Dictionary<ExpenseStatus, ReportStatusSummary> byStatus = summary.ByStatus.ToDictionary(item => item.Status);
        Assert.AreEqual(0, byStatus[ExpenseStatus.Draft].Count);
        Assert.AreEqual(0.00m, byStatus[ExpenseStatus.Draft].Amount);
        Assert.AreEqual(0, byStatus[ExpenseStatus.Submitted].Count);
        Assert.AreEqual(0.00m, byStatus[ExpenseStatus.Submitted].Amount);
        Assert.AreEqual(2, byStatus[ExpenseStatus.Approved].Count);
        Assert.AreEqual(150.00m, byStatus[ExpenseStatus.Approved].Amount);
        Assert.AreEqual(0, byStatus[ExpenseStatus.Rejected].Count);
        Assert.AreEqual(0.00m, byStatus[ExpenseStatus.Rejected].Amount);
        Assert.AreEqual(1, byStatus[ExpenseStatus.Paid].Count);
        Assert.AreEqual(25.00m, byStatus[ExpenseStatus.Paid].Amount);
    }

    [TestMethod]
    public void ReportJob_MarkCompletedTwice_ThrowsInvalidOperationException()
    {
        ReportJob job = ReportJob.CreatePending(
            "emp-1",
            ReportScope.Mine,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30),
            _generatedAt);

        ReportSummary summary = ReportSummary.Calculate([], _generatedAt);
        job.Complete(summary, _generatedAt);

        Assert.ThrowsExactly<InvalidOperationException>(() => job.Complete(summary, _generatedAt.AddSeconds(5)));
    }
}

