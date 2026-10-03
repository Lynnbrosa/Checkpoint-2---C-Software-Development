using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ExpensePaymentServiceTests
{
    private InMemoryExpenseRepository _repository = null!;
    private ExpensePaymentService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryExpenseRepository();
        _service = new ExpensePaymentService(_repository, new FixedTimeProvider(TestData.Now));
    }

    [TestMethod]
    public async Task PayAsync_ApprovedOfAnotherPerson_MovesToPaid()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        Assert.AreEqual(ExpenseStatus.Paid, ResultAssert.Succeeded(result).Status);
        Assert.AreEqual(1, _repository.SaveCount);
    }

    [TestMethod]
    public async Task PayAsync_RecordsPaymentWithServerActorTimeAndApprovedAmount()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        PaymentResponse? payment = ResultAssert.Succeeded(result).Payment;
        Assert.IsNotNull(payment);
        Assert.AreEqual(TestUsers.Finance.Id, payment.PaidById);
        Assert.AreEqual(TestData.Now.UtcDateTime, payment.PaidAt);
        Assert.AreEqual(expense.Amount, payment.Amount);
        Assert.IsNotNull(expense.Payment);
        Assert.AreEqual(expense.Id, expense.Payment.ExpenseId);
    }

    [TestMethod]
    public async Task PayAsync_AddsPaidEntryToTheSameAggregate()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));
        int before = expense.History.Count;

        await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        // estado, pagamento e histórico mudam juntos e saem num save só
        Assert.HasCount(before + 1, expense.History);
        ExpenseHistory entry = expense.History.Last();
        Assert.AreEqual(ExpenseAction.Paid, entry.Action);
        Assert.AreEqual(ExpenseStatus.Approved, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Paid, entry.ToStatus);
        Assert.AreEqual(TestUsers.Finance.Id, entry.ActorId);
        Assert.AreEqual(1, _repository.SaveCount);
    }

    [TestMethod]
    public async Task PayAsync_Twice_ReturnsConflictWithoutSecondPaymentOrHistory()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));
        await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);
        PaymentRecord? firstPayment = expense.Payment;
        int historyCount = expense.History.Count;

        ServiceResult<ExpenseResponse> second = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        ResultAssert.Failed(second, ServiceErrorKind.Conflict);
        Assert.AreSame(firstPayment, expense.Payment);
        Assert.HasCount(historyCount, expense.History);
    }

    [TestMethod]
    public async Task PayAsync_Submitted_ReturnsConflict()
    {
        Expense expense = Seed(TestData.Submitted(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
        Assert.IsNull(expense.Payment);
    }

    [TestMethod]
    public async Task PayAsync_Rejected_ReturnsConflict()
    {
        Expense expense = Seed(TestData.Rejected(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
        Assert.IsNull(expense.Payment);
    }

    [TestMethod]
    public async Task PayAsync_DraftOfAnotherPerson_ReturnsNotFound()
    {
        Expense expense = Seed(TestData.Draft(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task PayAsync_OwnExpenseEvenWithFinanceRole_ReturnsForbidden()
    {
        Expense own = Seed(TestData.Approved(TestUsers.EmployeeAndFinance));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(own.Id, TestUsers.EmployeeAndFinance, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.AreEqual(ExpenseStatus.Approved, own.Status);
        Assert.IsNull(own.Payment);
    }

    [TestMethod]
    [DataRow(Roles.Employee)]
    [DataRow(Roles.Approver)]
    [DataRow(Roles.Auditor)]
    [DataRow(Roles.Admin)]
    public async Task PayAsync_WithoutFinanceRole_ReturnsForbidden(string role)
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(
            expense.Id,
            new UserContext("user-" + role, [role]),
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.AreEqual(ExpenseStatus.Approved, expense.Status);
    }

    [TestMethod]
    public async Task PayAsync_WhenAnotherRequestPaidFirst_ReturnsConflict()
    {
        Expense expense = Seed(TestData.Approved(TestUsers.Employee));
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.PayAsync(expense.Id, TestUsers.Finance, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
        Assert.AreEqual(0, _repository.SaveCount);
    }

    [TestMethod]
    public async Task PayAsync_UnknownExpense_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.PayAsync(Guid.NewGuid(), TestUsers.Finance, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    private Expense Seed(Expense expense)
    {
        _repository.Seed(expense);
        return expense;
    }
}
