using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpenseWorkflowTests
{
    [TestMethod]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Updated, ExpenseStatus.Draft)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Submitted, ExpenseStatus.Submitted)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Approved, ExpenseStatus.Approved)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Rejected, ExpenseStatus.Rejected)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Paid, ExpenseStatus.Paid)]
    public void TryGetNextStatus_AllowedTransition_ReturnsNextStatus(
        ExpenseStatus current,
        ExpenseAction action,
        ExpenseStatus expected)
    {
        bool allowed = ExpenseWorkflow.TryGetNextStatus(current, action, out ExpenseStatus next);

        Assert.IsTrue(allowed);
        Assert.AreEqual(expected, next);
    }

    [TestMethod]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Draft, ExpenseAction.Created)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Submitted)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Approved)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Rejected)]
    [DataRow(ExpenseStatus.Submitted, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Paid)]
    [DataRow(ExpenseStatus.Approved, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Rejected, ExpenseAction.Updated)]
    [DataRow(ExpenseStatus.Paid, ExpenseAction.Updated)]
    public void TryGetNextStatus_ForbiddenTransition_ReturnsFalse(ExpenseStatus current, ExpenseAction action)
    {
        Assert.IsFalse(ExpenseWorkflow.TryGetNextStatus(current, action, out _));
    }
}
