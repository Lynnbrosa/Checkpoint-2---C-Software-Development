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
internal sealed class ExpenseHistoryServiceTests
{
    private InMemoryExpenseRepository _repository = null!;
    private ExpenseHistoryService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryExpenseRepository();
        _service = new ExpenseHistoryService(_repository);
    }

    [TestMethod]
    public async Task GetHistoryAsync_Owner_ReceivesEveryActionInOrder()
    {
        Expense expense = Seed(TestData.Paid(TestUsers.Employee));

        IReadOnlyList<ExpenseHistoryResponse> history = await HistoryAsAsync(expense, TestUsers.Employee);

        CollectionAssert.AreEqual(
            new[] { ExpenseAction.Created, ExpenseAction.Submitted, ExpenseAction.Approved, ExpenseAction.Paid },
            history.Select(entry => entry.Action).ToArray());
        Assert.IsNull(history[0].FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, history[^1].ToStatus);
    }

    [TestMethod]
    public async Task GetHistoryAsync_Rejection_ExposesTheJustification()
    {
        Expense expense = Seed(TestData.Rejected(TestUsers.Employee));

        IReadOnlyList<ExpenseHistoryResponse> history = await HistoryAsAsync(expense, TestUsers.Auditor);

        ExpenseHistoryResponse rejection = history.Single(entry => entry.Action == ExpenseAction.Rejected);
        Assert.AreEqual("Faltou o comprovante da despesa.", rejection.Justification);
    }

    [TestMethod]
    public async Task GetHistoryAsync_DraftEdit_ExposesChangedFields()
    {
        Expense expense = TestData.Draft(TestUsers.Employee);
        expense.UpdateDraft(
            new ExpenseCategory(1, "Transporte"),
            expense.Description,
            99.90m,
            expense.ExpenseDate,
            TestUsers.Employee.Id,
            TestData.Now.UtcDateTime);
        Seed(expense);

        IReadOnlyList<ExpenseHistoryResponse> history = await HistoryAsAsync(expense, TestUsers.Employee);

        FieldChangeResponse change = history.Single(entry => entry.Action == ExpenseAction.Updated).Changes.Single();
        Assert.AreEqual("amount", change.Field);
        Assert.AreEqual("120.00", change.From);
        Assert.AreEqual("99.90", change.To);
    }

    [TestMethod]
    public async Task GetHistoryAsync_AnotherEmployee_ReturnsNotFound()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(expense.Id, TestUsers.OtherEmployee, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task GetHistoryAsync_ApproverAfterApproval_ReturnsNotFoundLikeTheDetail()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(expense.Id, TestUsers.Approver, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task GetHistoryAsync_FinanceOnPaidExpense_IsVisible()
    {
        Expense expense = Seed(TestData.Paid(TestUsers.Employee));

        IReadOnlyList<ExpenseHistoryResponse> history = await HistoryAsAsync(expense, TestUsers.Finance);

        Assert.HasCount(4, history);
    }

    [TestMethod]
    public async Task GetHistoryAsync_AdminAlone_ReturnsForbidden()
    {
        Expense expense = Seed(TestData.Draft(TestUsers.Employee));

        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(expense.Id, TestUsers.Admin, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task GetHistoryAsync_UnknownExpense_ReturnsNotFound()
    {
        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(Guid.NewGuid(), TestUsers.Auditor, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    private async Task<IReadOnlyList<ExpenseHistoryResponse>> HistoryAsAsync(Expense expense, UserContext user)
    {
        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _service.GetHistoryAsync(expense.Id, user, CancellationToken.None);
        return ResultAssert.Succeeded(result);
    }

    private Expense Seed(Expense expense)
    {
        _repository.Seed(expense);
        return expense;
    }
}
