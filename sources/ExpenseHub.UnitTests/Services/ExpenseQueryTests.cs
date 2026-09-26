using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ExpenseQueryTests
{
    private InMemoryExpenseRepository _repository = null!;
    private ExpenseService _service = null!;
    private Expense _myDraft = null!;
    private Expense _mySubmitted = null!;
    private Expense _otherDraft = null!;
    private Expense _otherSubmitted = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryExpenseRepository();
        _service = new ExpenseService(_repository, new FixedTimeProvider(TestData.Now));

        _myDraft = TestData.Draft(TestUsers.Employee);
        _mySubmitted = TestData.Submitted(TestUsers.Employee);
        _otherDraft = TestData.Draft(TestUsers.OtherEmployee);
        _otherSubmitted = TestData.Submitted(TestUsers.OtherEmployee);
        foreach (Expense expense in new[] { _myDraft, _mySubmitted, _otherDraft, _otherSubmitted })
        {
            _repository.Seed(expense);
        }
    }

    [TestMethod]
    public async Task ListAsync_AsEmployee_ReturnsOnlyOwnExpenses()
    {
        IReadOnlyList<Guid> ids = await ListIdsAsync(TestUsers.Employee);

        CollectionAssert.AreEquivalent(new[] { _myDraft.Id, _mySubmitted.Id }, ids.ToArray());
    }

    [TestMethod]
    public async Task ListAsync_AsApprover_ReturnsOnlySubmitted()
    {
        IReadOnlyList<Guid> ids = await ListIdsAsync(TestUsers.Approver);

        CollectionAssert.AreEquivalent(new[] { _mySubmitted.Id, _otherSubmitted.Id }, ids.ToArray());
    }

    [TestMethod]
    public async Task ListAsync_AsFinance_DoesNotSeeDraftsOrSubmitted()
    {
        IReadOnlyList<Guid> ids = await ListIdsAsync(TestUsers.Finance);

        Assert.IsEmpty(ids);
    }

    [TestMethod]
    public async Task ListAsync_AsAuditor_ReturnsEverything()
    {
        IReadOnlyList<Guid> ids = await ListIdsAsync(TestUsers.Auditor);

        Assert.HasCount(4, ids);
    }

    [TestMethod]
    public async Task ListAsync_EmployeeAndApprover_GetsTheUnionButNotOtherDrafts()
    {
        Expense ownerDraft = TestData.Draft(TestUsers.EmployeeAndApprover);
        _repository.Seed(ownerDraft);

        IReadOnlyList<Guid> ids = await ListIdsAsync(TestUsers.EmployeeAndApprover);

        CollectionAssert.AreEquivalent(new[] { ownerDraft.Id, _mySubmitted.Id, _otherSubmitted.Id }, ids.ToArray());
    }

    [TestMethod]
    public async Task ListAsync_AsAdminOnly_ReturnsForbidden()
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(TestUsers.Admin, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task ListAsync_WithoutRoles_ReturnsForbidden()
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(TestUsers.WithoutRoles, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task GetAsync_OwnExpense_ReturnsIt()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(_myDraft.Id, TestUsers.Employee, CancellationToken.None);

        Assert.AreEqual(_myDraft.Id, ResultAssert.Succeeded(result).Id);
    }

    [TestMethod]
    public async Task GetAsync_ExpenseOfAnotherEmployee_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(_otherSubmitted.Id, TestUsers.Employee, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task GetAsync_DraftAsApprover_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(_otherDraft.Id, TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task GetAsync_UnknownId_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.GetAsync(Guid.NewGuid(), TestUsers.Auditor, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    private async Task<IReadOnlyList<Guid>> ListIdsAsync(UserContext user)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _service.ListAsync(user, CancellationToken.None);
        return ResultAssert.Succeeded(result).Select(expense => expense.Id).ToList();
    }
}
