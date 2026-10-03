using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ExpenseDecisionServiceTests
{
    private const string ValidJustification = "Nota fiscal ilegível, reenviar com comprovante.";

    private InMemoryExpenseRepository _repository = null!;
    private ExpenseDecisionService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryExpenseRepository();
        _service = new ExpenseDecisionService(_repository, new FixedTimeProvider(TestData.Now));
    }

    [TestMethod]
    public async Task ApproveAsync_SubmittedOfAnotherPerson_MovesToApprovedWithServerActorAndTime()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(expense.Id, TestUsers.Approver, CancellationToken.None);

        Assert.AreEqual(ExpenseStatus.Approved, ResultAssert.Succeeded(result).Status);
        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Approved, entry.Action);
        Assert.AreEqual(TestUsers.Approver.Id, entry.ActorId);
        Assert.AreEqual(TestData.Now.UtcDateTime, entry.OccurredAt);
        Assert.AreEqual(ExpenseStatus.Submitted, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Approved, entry.ToStatus);
    }

    [TestMethod]
    public async Task RejectAsync_SubmittedOfAnotherPerson_MovesToRejectedAndPersistsJustification()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            expense.Id,
            Justification(ValidJustification),
            TestUsers.Approver,
            CancellationToken.None);

        ExpenseResponse rejected = ResultAssert.Succeeded(result);
        Assert.AreEqual(ExpenseStatus.Rejected, rejected.Status);
        Assert.AreEqual(ValidJustification, rejected.RejectionReason);
        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Rejected, entry.Action);
        Assert.AreEqual(ValidJustification, entry.Justification);
        Assert.AreEqual(TestUsers.Approver.Id, entry.ActorId);
        Assert.AreEqual(ExpenseStatus.Rejected, entry.ToStatus);
    }

    [TestMethod]
    public async Task ApproveAsync_OwnExpenseEvenWithApproverRole_ReturnsForbidden()
    {
        Expense own = Seed(TestData.Submitted(TestUsers.EmployeeAndApprover));

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(own.Id, TestUsers.EmployeeAndApprover, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.AreEqual(ExpenseStatus.Submitted, own.Status);
    }

    [TestMethod]
    public async Task RejectAsync_OwnExpenseEvenWithApproverRole_ReturnsForbidden()
    {
        Expense own = Seed(TestData.Submitted(TestUsers.EmployeeAndApprover));

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            own.Id,
            Justification(ValidJustification),
            TestUsers.EmployeeAndApprover,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.IsNull(own.RejectionReason);
    }

    [TestMethod]
    [DataRow(Roles.Employee)]
    [DataRow(Roles.Finance)]
    [DataRow(Roles.Auditor)]
    [DataRow(Roles.Admin)]
    public async Task ApproveAsync_WithoutApproverRole_ReturnsForbidden(string role)
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(
            expense.Id,
            new UserContext("user-" + role, [role]),
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    [TestMethod]
    public async Task ApproveAsync_Twice_ReturnsConflictWithoutDuplicatingHistory()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));
        await _service.ApproveAsync(expense.Id, TestUsers.Approver, CancellationToken.None);
        int historyCount = expense.History.Count;

        ServiceResult<ExpenseResponse> second = await _service.ApproveAsync(expense.Id, TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(second, ServiceErrorKind.Conflict);
        Assert.HasCount(historyCount, expense.History);
    }

    [TestMethod]
    public async Task RejectAsync_AfterApproval_ReturnsConflict()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            expense.Id,
            Justification(ValidJustification),
            TestUsers.Approver,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
    }

    [TestMethod]
    public async Task ApproveAsync_AfterRejection_ReturnsConflict()
    {
        Expense expense = Seed(TestData.Rejected(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(expense.Id, TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
        Assert.AreEqual(ExpenseStatus.Rejected, expense.Status);
    }

    [TestMethod]
    public async Task ApproveAsync_DraftOfAnotherPerson_ReturnsNotFound()
    {
        Expense draft = Seed(TestData.Draft(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(draft.Id, TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task ApproveAsync_UnknownExpense_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(Guid.NewGuid(), TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("          ")]
    [DataRow("curta")]
    [DataRow("123456789")]
    public async Task RejectAsync_InvalidJustification_ReturnsValidationAndKeepsSubmitted(string justification)
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            expense.Id,
            Justification(justification),
            TestUsers.Approver,
            CancellationToken.None);

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("Justification", error.Field);
        Assert.AreEqual(ExpenseStatus.Submitted, expense.Status);
    }

    [TestMethod]
    public async Task RejectAsync_JustificationTooLong_ReturnsValidation()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            expense.Id,
            Justification(new string('x', 501)),
            TestUsers.Approver,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Validation);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public async Task RejectAsync_JustificationOnTheLimits_IsAccepted(int length)
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.RejectAsync(
            expense.Id,
            Justification(new string('x', length)),
            TestUsers.Approver,
            CancellationToken.None);

        ResultAssert.Succeeded(result);
    }

    [TestMethod]
    public async Task ApproveAsync_WhenAnotherApproverDecidedFirst_ReturnsConflict()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.ApproveAsync(expense.Id, TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
    }

    [TestMethod]
    public void ReadableBy_FinanceSeesApprovedButApproverDoesNot()
    {
        Expense approved = TestData.Approved(TestUsers.Employee);
        Expense rejected = TestData.Rejected(TestUsers.Employee);
        IQueryable<Expense> all = new List<Expense> { approved, rejected }.AsQueryable();

        CollectionAssert.AreEqual(new[] { approved }, all.Where(ExpenseAccessPolicy.ReadableBy(TestUsers.Finance)).ToArray());
        Assert.IsEmpty(all.Where(ExpenseAccessPolicy.ReadableBy(TestUsers.Approver)).ToArray());
        Assert.HasCount(2, all.Where(ExpenseAccessPolicy.ReadableBy(TestUsers.Auditor)).ToArray());
    }

    private static RejectExpenseRequest Justification(string text) => new() { Justification = text };

    private Expense Seed(Expense expense)
    {
        _repository.Seed(expense);
        return expense;
    }
}
