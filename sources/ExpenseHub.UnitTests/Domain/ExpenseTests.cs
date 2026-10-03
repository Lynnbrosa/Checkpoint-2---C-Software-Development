using System;
using ExpenseHub.Api.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Domain;

[TestClass]
internal sealed class ExpenseTests
{
    private static readonly ExpenseCategory _category = new(1, "Transporte");

    [TestMethod]
    public void CreateDraft_StartsInDraftWithServerGeneratedData()
    {
        Expense expense = Expense.CreateDraft("owner-1", _category, "Estacionamento do evento", 35m, TestData.Today, TestData.Now.UtcDateTime);

        Assert.AreNotEqual(Guid.Empty, expense.Id);
        Assert.AreEqual(ExpenseStatus.Draft, expense.Status);
        Assert.AreEqual("owner-1", expense.OwnerId);
        Assert.AreEqual(TestData.Now.UtcDateTime, expense.CreatedAt);
        Assert.AreNotEqual(Guid.Empty, expense.ConcurrencyStamp);
    }

    [TestMethod]
    public void CreateDraft_WithInvalidDescription_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Expense.CreateDraft("owner-1", _category, "curta", 35m, TestData.Today, TestData.Now.UtcDateTime));
    }

    [TestMethod]
    public void CreateDraft_WithInvalidAmount_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Expense.CreateDraft("owner-1", _category, "Estacionamento do evento", 0m, TestData.Today, TestData.Now.UtcDateTime));
    }

    [TestMethod]
    public void Reject_WithBlankJustification_Throws()
    {
        Expense expense = TestData.Submitted(TestUsers.Employee);

        Assert.ThrowsExactly<ArgumentException>(() => expense.Reject("approver-1", "          ", TestData.Now.UtcDateTime));
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    [TestMethod]
    public void Approve_OutsideSubmitted_Throws()
    {
        Expense expense = TestData.Draft(TestUsers.Employee);

        Assert.ThrowsExactly<InvalidOperationException>(() => expense.Approve("approver-1", TestData.Now.UtcDateTime));
    }

    [TestMethod]
    public void UpdateDraft_ChangesConcurrencyStamp()
    {
        Expense expense = TestData.Draft(TestUsers.Employee);
        Guid before = expense.ConcurrencyStamp;

        expense.UpdateDraft(_category, "Estacionamento do evento", 40m, TestData.Today, TestUsers.Employee.Id, TestData.Now.UtcDateTime);

        Assert.AreNotEqual(before, expense.ConcurrencyStamp);
        Assert.AreEqual(TestData.Now.UtcDateTime, expense.UpdatedAt);
    }
}
