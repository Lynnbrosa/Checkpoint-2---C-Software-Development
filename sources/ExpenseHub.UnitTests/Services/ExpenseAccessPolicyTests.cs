using System;
using System.Linq;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ExpenseAccessPolicyTests
{
    [TestMethod]
    [DataRow(ExpenseOperation.Edit, Roles.Employee)]
    [DataRow(ExpenseOperation.Submit, Roles.Employee)]
    [DataRow(ExpenseOperation.Decide, Roles.Approver)]
    [DataRow(ExpenseOperation.Pay, Roles.Finance)]
    public void RequiredRole_FollowsTheMatrix(ExpenseOperation operation, string expectedRole)
    {
        Assert.AreEqual(expectedRole, ExpenseAccessPolicy.RequiredRole(operation));
    }

    [TestMethod]
    public void HasReadRole_AdminAlone_IsFalse()
    {
        Assert.IsFalse(ExpenseAccessPolicy.HasReadRole(TestUsers.Admin));
        Assert.IsFalse(ExpenseAccessPolicy.HasReadRole(TestUsers.WithoutRoles));
    }

    [TestMethod]
    public void OwnershipAllows_OwnerEditsAndSubmitsButNeverDecidesOrPays()
    {
        Expense own = TestData.Draft(TestUsers.EmployeeAndApprover);

        Assert.IsTrue(ExpenseAccessPolicy.OwnershipAllows(own, TestUsers.EmployeeAndApprover, ExpenseOperation.Edit));
        Assert.IsTrue(ExpenseAccessPolicy.OwnershipAllows(own, TestUsers.EmployeeAndApprover, ExpenseOperation.Submit));
        Assert.IsFalse(ExpenseAccessPolicy.OwnershipAllows(own, TestUsers.EmployeeAndApprover, ExpenseOperation.Decide));
        Assert.IsFalse(ExpenseAccessPolicy.OwnershipAllows(own, TestUsers.EmployeeAndApprover, ExpenseOperation.Pay));
    }

    [TestMethod]
    public void OwnershipAllows_NonOwnerDecidesAndPaysButNeverEdits()
    {
        Expense other = TestData.Submitted(TestUsers.Employee);

        Assert.IsFalse(ExpenseAccessPolicy.OwnershipAllows(other, TestUsers.EmployeeAndApprover, ExpenseOperation.Edit));
        Assert.IsFalse(ExpenseAccessPolicy.OwnershipAllows(other, TestUsers.EmployeeAndApprover, ExpenseOperation.Submit));
        Assert.IsTrue(ExpenseAccessPolicy.OwnershipAllows(other, TestUsers.Approver, ExpenseOperation.Decide));
        Assert.IsTrue(ExpenseAccessPolicy.OwnershipAllows(other, TestUsers.Finance, ExpenseOperation.Pay));
    }

    [TestMethod]
    public void ReadableBy_AdminAlone_SeesNothing()
    {
        Expense[] all = [TestData.Draft(TestUsers.Employee), TestData.Submitted(TestUsers.Employee)];

        Assert.IsEmpty(Matches(all, ExpenseAccessPolicy.ReadableBy(TestUsers.Admin)));
    }

    [TestMethod]
    public void ReadableBy_AdminAndEmployee_SeesOnlyOwnLikeAnyEmployee()
    {
        Expense own = TestData.Draft(TestUsers.AdminAndEmployee);
        Expense other = TestData.Submitted(TestUsers.Employee);

        Expense[] visible = Matches([own, other], ExpenseAccessPolicy.ReadableBy(TestUsers.AdminAndEmployee));

        CollectionAssert.AreEqual(new[] { own }, visible);
    }

    [TestMethod]
    public void ActionableBy_DecideAsApprover_IncludesSubmittedButNotDraftsOfOthers()
    {
        Expense otherDraft = TestData.Draft(TestUsers.Employee);
        Expense otherSubmitted = TestData.Submitted(TestUsers.Employee);

        Expense[] found = Matches(
            [otherDraft, otherSubmitted],
            ExpenseAccessPolicy.ActionableBy(TestUsers.Approver, ExpenseOperation.Decide));

        CollectionAssert.AreEqual(new[] { otherSubmitted }, found);
    }

    [TestMethod]
    public void ActionableBy_PayAsFinance_IncludesSubmittedSoTheStateCheckAnswers409()
    {
        Expense otherDraft = TestData.Draft(TestUsers.Employee);
        Expense otherSubmitted = TestData.Submitted(TestUsers.Employee);

        Expense[] found = Matches(
            [otherDraft, otherSubmitted],
            ExpenseAccessPolicy.ActionableBy(TestUsers.Finance, ExpenseOperation.Pay));

        CollectionAssert.AreEqual(new[] { otherSubmitted }, found);
    }

    [TestMethod]
    public void ActionableBy_EditAsEmployee_IsTheSameAsReading()
    {
        Expense own = TestData.Draft(TestUsers.Employee);
        Expense other = TestData.Draft(TestUsers.OtherEmployee);

        Expense[] found = Matches([own, other], ExpenseAccessPolicy.ActionableBy(TestUsers.Employee, ExpenseOperation.Edit));

        CollectionAssert.AreEqual(new[] { own }, found);
    }

    private static Expense[] Matches(Expense[] expenses, Expression<Func<Expense, bool>> scope) =>
        expenses.AsQueryable().Where(scope).ToArray();
}
