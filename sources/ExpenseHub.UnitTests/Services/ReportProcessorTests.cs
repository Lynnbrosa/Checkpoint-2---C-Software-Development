using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services.Reports;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ReportProcessorTests
{
    private static readonly DateOnly _fromDate = new(2026, 9, 1);
    private static readonly DateOnly _toDate = new(2026, 9, 30);

    [TestMethod]
    public async Task ProcessAsync_MineVsGlobal_FiltersByOwnerAndInclusiveExpenseDate()
    {
        InMemoryReportRepository repo = new();
        FixedTimeProvider clock = new(TestData.Now);
        ReportProcessor processor = new(repo, clock, NullLogger<ReportProcessor>.Instance);

        ExpenseCategory category = new(1, "Transporte");
        DateTime created = TestData.Now.UtcDateTime.AddDays(-5);

        // Limite inferior inclusivo (2026-09-01)
        Expense approved1 = Expense.CreateDraft(TestUsers.Employee.Id, category, "Reembolso no primeiro dia", 100.00m, _fromDate, created);
        approved1.Submit(TestUsers.Employee.Id, created);
        approved1.Approve(TestUsers.Approver.Id, created);

        // Limite superior inclusivo (2026-09-30)
        Expense approved2 = Expense.CreateDraft(TestUsers.Employee.Id, category, "Reembolso no último dia", 50.00m, _toDate, created);
        approved2.Submit(TestUsers.Employee.Id, created);
        approved2.Approve(TestUsers.Approver.Id, created);

        // Pago dentro do período
        Expense paid = Expense.CreateDraft(TestUsers.Employee.Id, category, "Reembolso pago no período", 25.00m, new DateOnly(2026, 9, 15), created);
        paid.Submit(TestUsers.Employee.Id, created);
        paid.Approve(TestUsers.Approver.Id, created);
        paid.Pay(TestUsers.Finance.Id, created);

        // De outro funcionário dentro do período
        Expense foreign = Expense.CreateDraft(TestUsers.OtherEmployee.Id, category, "Reembolso de outro funcionário", 200.00m, new DateOnly(2026, 9, 10), created);
        foreign.Submit(TestUsers.OtherEmployee.Id, created);
        foreign.Approve(TestUsers.Approver.Id, created);

        // Do solicitante, mas fora do período (mesmo criado hoje, ExpenseDate é 2026-10-01)
        Expense outOfPeriod = Expense.CreateDraft(TestUsers.Employee.Id, category, "Reembolso fora do período", 500.00m, new DateOnly(2026, 10, 1), created);
        outOfPeriod.Submit(TestUsers.Employee.Id, created);
        outOfPeriod.Approve(TestUsers.Approver.Id, created);
        outOfPeriod.Pay(TestUsers.Finance.Id, created);

        repo.SeedExpense(approved1);
        repo.SeedExpense(approved2);
        repo.SeedExpense(paid);
        repo.SeedExpense(foreign);
        repo.SeedExpense(outOfPeriod);

        ReportJob mineJob = ReportJob.CreatePending(TestUsers.Employee.Id, ReportScope.Mine, _fromDate, _toDate, clock.GetUtcNow().UtcDateTime);
        ReportJob globalJob = ReportJob.CreatePending(TestUsers.Auditor.Id, ReportScope.Global, _fromDate, _toDate, clock.GetUtcNow().UtcDateTime);
        repo.SeedJob(mineJob);
        repo.SeedJob(globalJob);

        clock.Advance(TimeSpan.FromSeconds(2));
        Assert.AreEqual(ReportProcessingOutcome.Completed, await processor.ProcessAsync(mineJob.Id, CancellationToken.None));
        Assert.AreEqual(ReportProcessingOutcome.Completed, await processor.ProcessAsync(globalJob.Id, CancellationToken.None));

        ReportSummary? mineSummary = mineJob.GetSummary();
        Assert.IsNotNull(mineSummary);
        Assert.AreEqual(3, mineSummary.TotalCount);
        Assert.AreEqual(175.00m, mineSummary.TotalAmount);
        Assert.AreEqual(150.00m, mineSummary.ByStatus.Single(s => s.Status == ExpenseStatus.Approved).Amount);
        Assert.AreEqual(25.00m, mineSummary.ByStatus.Single(s => s.Status == ExpenseStatus.Paid).Amount);

        ReportSummary? globalSummary = globalJob.GetSummary();
        Assert.IsNotNull(globalSummary);
        Assert.AreEqual(4, globalSummary.TotalCount);
        Assert.AreEqual(375.00m, globalSummary.TotalAmount);
        Assert.AreEqual(350.00m, globalSummary.ByStatus.Single(s => s.Status == ExpenseStatus.Approved).Amount);
        Assert.AreEqual(25.00m, globalSummary.ByStatus.Single(s => s.Status == ExpenseStatus.Paid).Amount);
    }

    [TestMethod]
    public async Task ProcessAsync_DuplicateDeliveryAfterCompletion_DoesNotRecalculateOrOverwriteTimestamp()
    {
        InMemoryReportRepository repo = new();
        FixedTimeProvider clock = new(TestData.Now);
        ReportProcessor processor = new(repo, clock, NullLogger<ReportProcessor>.Instance);

        ExpenseCategory category = new(1, "Transporte");
        Expense draft = Expense.CreateDraft(TestUsers.Employee.Id, category, "Despesa inicial no período", 80.00m, new DateOnly(2026, 9, 10), clock.GetUtcNow().UtcDateTime);
        repo.SeedExpense(draft);

        ReportJob job = ReportJob.CreatePending(TestUsers.Employee.Id, ReportScope.Mine, _fromDate, _toDate, clock.GetUtcNow().UtcDateTime);
        repo.SeedJob(job);

        clock.Advance(TimeSpan.FromSeconds(3));
        DateTime firstCompletedAt = clock.GetUtcNow().UtcDateTime;

        ReportProcessingOutcome firstOutcome = await processor.ProcessAsync(job.Id, CancellationToken.None);
        Assert.AreEqual(ReportProcessingOutcome.Completed, firstOutcome);
        Assert.AreEqual(1, repo.EligibleQueryCount);
        Assert.AreEqual(firstCompletedAt, job.CompletedAt);

        // Altera o reembolso e avança o relógio; depois simula novo worker recebendo mensagem duplicada
        draft.Submit(TestUsers.Employee.Id, clock.GetUtcNow().UtcDateTime);
        clock.Advance(TimeSpan.FromMinutes(10));

        ReportProcessor restartedWorkerProcessor = new(repo, clock, NullLogger<ReportProcessor>.Instance);
        ReportProcessingOutcome secondOutcome = await restartedWorkerProcessor.ProcessAsync(job.Id, CancellationToken.None);

        Assert.AreEqual(ReportProcessingOutcome.DuplicateIgnored, secondOutcome);
        Assert.AreEqual(1, repo.EligibleQueryCount);
        Assert.AreEqual(firstCompletedAt, job.CompletedAt);

        ReportSummary? summary = job.GetSummary();
        Assert.IsNotNull(summary);
        Assert.AreEqual(firstCompletedAt, summary.GeneratedAt);
        Assert.AreEqual(1, summary.ByStatus.Single(s => s.Status == ExpenseStatus.Draft).Count);
        Assert.AreEqual(0, summary.ByStatus.Single(s => s.Status == ExpenseStatus.Submitted).Count);
    }

    [TestMethod]
    public async Task GetAndDownloadExcel_AfterExpenseMutated_ReturnsPersistedSnapshotWithoutRecalculation()
    {
        InMemoryReportRepository repo = new();
        FakeReportMessagePublisher publisher = new();
        FixedTimeProvider clock = new(TestData.Now);
        ReportService service = new(repo, publisher, clock, NullLogger<ReportService>.Instance);
        ReportProcessor processor = new(repo, clock, NullLogger<ReportProcessor>.Instance);

        ExpenseCategory category = new(1, "Transporte");
        Expense expense = Expense.CreateDraft(TestUsers.Employee.Id, category, "Despesa original rascunho", 120.00m, new DateOnly(2026, 9, 10), clock.GetUtcNow().UtcDateTime);
        repo.SeedExpense(expense);

        ReportResponse requested = ResultAssert.Succeeded(
            await service.RequestAsync(
                new CreateReportRequest { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Mine },
                TestUsers.Employee,
                CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(5));
        DateTime generatedAt = clock.GetUtcNow().UtcDateTime;
        await processor.ProcessAsync(requested.Id, CancellationToken.None);

        // Muda o reembolso depois que o relatório já foi concluído
        expense.UpdateDraft(category, "Despesa alterada para outro valor", 999.00m, new DateOnly(2026, 9, 10), TestUsers.Employee.Id, clock.GetUtcNow().UtcDateTime);
        expense.Submit(TestUsers.Employee.Id, clock.GetUtcNow().UtcDateTime);
        repo.SeedExpense(Expense.CreateDraft(TestUsers.Employee.Id, category, "Nova despesa adicionada depois", 300.00m, new DateOnly(2026, 9, 12), clock.GetUtcNow().UtcDateTime));

        int queriesBeforeGet = repo.EligibleQueryCount;

        ReportResponse detail = ResultAssert.Succeeded(
            await service.GetAsync(requested.Id, TestUsers.Employee, CancellationToken.None));

        ReportExcelFile excel = ResultAssert.Succeeded(
            await service.DownloadExcelAsync(requested.Id, TestUsers.Employee, CancellationToken.None));

        Assert.AreEqual(queriesBeforeGet, repo.EligibleQueryCount);
        Assert.IsNotNull(detail.Result);
        Assert.AreEqual(generatedAt, detail.Result.GeneratedAt);
        Assert.AreEqual(1, detail.Result.TotalCount);
        Assert.AreEqual(120.00m, detail.Result.TotalAmount);
        Assert.AreEqual(120.00m, detail.Result.ByStatus.Single(s => s.Status == ExpenseStatus.Draft).Amount);
        Assert.AreEqual(0.00m, detail.Result.ByStatus.Single(s => s.Status == ExpenseStatus.Submitted).Amount);
        Assert.IsNotEmpty(excel.Content);
    }

    [TestMethod]
    public async Task ProcessAsync_TransientSaveFailure_DoesNotConfirmAndAllowsRecoveryOnRedelivery()
    {
        InMemoryReportRepository repo = new();
        FixedTimeProvider clock = new(TestData.Now);
        ReportProcessor processor = new(repo, clock, NullLogger<ReportProcessor>.Instance);

        ReportJob job = ReportJob.CreatePending(TestUsers.Employee.Id, ReportScope.Mine, _fromDate, _toDate, clock.GetUtcNow().UtcDateTime);
        repo.SeedJob(job);

        // Simula falha de persistência ao tentar avançar o estado
        repo.FailNextSave = true;
        ReportProcessingOutcome failedAttempt = await processor.ProcessAsync(job.Id, CancellationToken.None);
        Assert.AreEqual(ReportProcessingOutcome.TransientFailure, failedAttempt);
        Assert.IsFalse(job.IsTerminal);

        // Na reentrega, o job em Processing deve ser retomado e concluído
        ReportProcessingOutcome retryAttempt = await processor.ProcessAsync(job.Id, CancellationToken.None);
        Assert.AreEqual(ReportProcessingOutcome.Completed, retryAttempt);
        Assert.AreEqual(ReportStatus.Completed, job.Status);
        Assert.IsNotNull(job.GetSummary());
    }
}

