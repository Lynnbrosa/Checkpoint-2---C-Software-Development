using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ExpenseServiceSubmitTests
{
    private InMemoryExpenseRepository _repository = null!;
    private ExpenseService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryExpenseRepository();
        _service = new ExpenseService(_repository, new FixedTimeProvider(TestData.Now));
    }

    [TestMethod]
    public async Task SubmitAsync_OwnDraft_MovesToSubmittedAndRecordsHistory()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(draft.Id, TestUsers.Employee, CancellationToken.None);

        Assert.AreEqual(ExpenseStatus.Submitted, ResultAssert.Succeeded(result).Status);
        ExpenseHistory entry = draft.History.Last();
        Assert.AreEqual(ExpenseAction.Submitted, entry.Action);
        Assert.AreEqual(ExpenseStatus.Draft, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Submitted, entry.ToStatus);
        Assert.AreEqual(TestUsers.Employee.Id, entry.ActorId);
        Assert.AreEqual(TestData.Now.UtcDateTime, entry.OccurredAt);
        Assert.AreEqual(1, _repository.SaveCount);
    }

    [TestMethod]
    public async Task SubmitAsync_Twice_ReturnsConflictWithoutDuplicatingHistory()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);
        await _service.SubmitAsync(draft.Id, TestUsers.Employee, CancellationToken.None);
        int historyAfterFirstSubmit = draft.History.Count;

        ServiceResult<ExpenseResponse> second = await _service.SubmitAsync(draft.Id, TestUsers.Employee, CancellationToken.None);

        ResultAssert.Failed(second, ServiceErrorKind.Conflict);
        Assert.HasCount(historyAfterFirstSubmit, draft.History);
        Assert.AreEqual(ExpenseStatus.Submitted, draft.Status);
    }

    [TestMethod]
    public async Task SubmitAsync_DraftOfAnotherEmployee_ReturnsNotFound()
    {
        Expense draft = TestData.Draft(TestUsers.OtherEmployee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(draft.Id, TestUsers.Employee, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
        Assert.AreEqual(ExpenseStatus.Draft, draft.Status);
    }

    [TestMethod]
    public async Task SubmitAsync_UnknownExpense_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(Guid.NewGuid(), TestUsers.Employee, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task SubmitAsync_AsAuditor_ReturnsForbidden()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(draft.Id, TestUsers.Auditor, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.AreEqual(ExpenseStatus.Draft, draft.Status);
    }

    [TestMethod]
    public async Task UpdateAsync_AfterSubmit_ReturnsConflict()
    {
        Expense submitted = TestData.Submitted(TestUsers.Employee);
        _repository.Seed(submitted);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            submitted.Id,
            TestData.DraftRequest(description: "Mudando depois de enviar"),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
        Assert.AreEqual("Almoço com cliente em São Paulo", submitted.Description);
    }
}
