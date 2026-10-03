using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

// o fluxo inteiro passando pelos quatro serviços em cima do mesmo repositório
[TestClass]
internal sealed class ExpenseLifecycleTests
{
    private InMemoryExpenseRepository _repository = null!;
    private ExpenseService _expenses = null!;
    private ExpenseDecisionService _decisions = null!;
    private ExpensePaymentService _payments = null!;
    private ExpenseHistoryService _history = null!;

    [TestInitialize]
    public void Setup()
    {
        FixedTimeProvider clock = new(TestData.Now);
        _repository = new InMemoryExpenseRepository();
        _expenses = new ExpenseService(_repository, clock);
        _decisions = new ExpenseDecisionService(_repository, clock);
        _payments = new ExpensePaymentService(_repository, clock);
        _history = new ExpenseHistoryService(_repository);
    }

    [TestMethod]
    public async Task FullFlow_RecordsOneEntryPerStepWithTheRightActor()
    {
        Guid id = await RunFullFlowAsync();

        ExpenseHistoryResponse[] history = await HistoryAsync(id);

        CollectionAssert.AreEqual(
            new[] { ExpenseAction.Created, ExpenseAction.Updated, ExpenseAction.Submitted, ExpenseAction.Approved, ExpenseAction.Paid },
            history.Select(entry => entry.Action).ToArray());
        CollectionAssert.AreEqual(
            new[] { TestUsers.Employee.Id, TestUsers.Employee.Id, TestUsers.Employee.Id, TestUsers.Approver.Id, TestUsers.Finance.Id },
            history.Select(entry => entry.ActorId).ToArray());
        Assert.IsTrue(history.All(entry => entry.OccurredAt == TestData.Now.UtcDateTime));
    }

    [TestMethod]
    public async Task FullFlow_EachEntryStartsWhereThePreviousEnded()
    {
        Guid id = await RunFullFlowAsync();

        ExpenseHistoryResponse[] history = await HistoryAsync(id);

        for (int i = 1; i < history.Length; i++)
        {
            Assert.AreEqual(history[i - 1].ToStatus, history[i].FromStatus, $"quebra entre {history[i - 1].Action} e {history[i].Action}");
        }
    }

    [TestMethod]
    public async Task RepeatingAnyStepAfterPayment_ReturnsConflictAndLeavesHistoryUntouched()
    {
        Guid id = await RunFullFlowAsync();
        int before = (await HistoryAsync(id)).Length;

        ServiceResult<ExpenseResponse>[] attempts =
        [
            await _expenses.UpdateAsync(id, TestData.DraftRequest(), TestUsers.Employee, CancellationToken.None),
            await _expenses.SubmitAsync(id, TestUsers.Employee, CancellationToken.None),
            await _decisions.ApproveAsync(id, TestUsers.Approver, CancellationToken.None),
            await _decisions.RejectAsync(id, new RejectExpenseRequest { Justification = "Tarde demais para reprovar." }, TestUsers.Approver, CancellationToken.None),
            await _payments.PayAsync(id, TestUsers.Finance, CancellationToken.None),
        ];

        foreach (ServiceResult<ExpenseResponse> attempt in attempts)
        {
            ResultAssert.Failed(attempt, ServiceErrorKind.Conflict);
        }

        Assert.HasCount(before, await HistoryAsync(id));
    }

    [TestMethod]
    public async Task RejectedExpense_CannotBePaidOrApprovedLater()
    {
        ExpenseResponse created = ResultAssert.Succeeded(
            await _expenses.CreateAsync(TestData.DraftRequest(), TestUsers.Employee, CancellationToken.None));
        await _expenses.SubmitAsync(created.Id, TestUsers.Employee, CancellationToken.None);
        await _decisions.RejectAsync(
            created.Id,
            new RejectExpenseRequest { Justification = "Despesa fora da política de viagem." },
            TestUsers.Approver,
            CancellationToken.None);

        ResultAssert.Failed(await _payments.PayAsync(created.Id, TestUsers.Finance, CancellationToken.None), ServiceErrorKind.Conflict);
        ResultAssert.Failed(await _decisions.ApproveAsync(created.Id, TestUsers.Approver, CancellationToken.None), ServiceErrorKind.Conflict);
    }

    private async Task<Guid> RunFullFlowAsync()
    {
        ExpenseResponse created = ResultAssert.Succeeded(
            await _expenses.CreateAsync(TestData.DraftRequest(), TestUsers.Employee, CancellationToken.None));

        ResultAssert.Succeeded(await _expenses.UpdateAsync(
            created.Id,
            TestData.DraftRequest(amount: 95.00m),
            TestUsers.Employee,
            CancellationToken.None));
        ResultAssert.Succeeded(await _expenses.SubmitAsync(created.Id, TestUsers.Employee, CancellationToken.None));
        ResultAssert.Succeeded(await _decisions.ApproveAsync(created.Id, TestUsers.Approver, CancellationToken.None));
        ResultAssert.Succeeded(await _payments.PayAsync(created.Id, TestUsers.Finance, CancellationToken.None));

        return created.Id;
    }

    private async Task<ExpenseHistoryResponse[]> HistoryAsync(Guid id) =>
        ResultAssert.Succeeded(await _history.GetHistoryAsync(id, TestUsers.Auditor, CancellationToken.None)).ToArray();
}
