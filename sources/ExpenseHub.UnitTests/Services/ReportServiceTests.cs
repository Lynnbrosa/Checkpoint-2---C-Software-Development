using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Services.Reports;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class ReportServiceTests
{
    private static readonly DateOnly _fromDate = new(2026, 9, 1);
    private static readonly DateOnly _toDate = new(2026, 9, 30);

    [TestMethod]
    public async Task RequestAsync_EmployeeRequestsMine_PersistsPendingAndPublishesMessage()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();

        CreateReportRequest request = new()
        {
            FromDate = _fromDate,
            ToDate = _toDate,
            Scope = ReportScope.Mine,
        };

        ReportResponse response = ResultAssert.Succeeded(
            await service.RequestAsync(request, TestUsers.Employee, CancellationToken.None));

        Assert.AreEqual(ReportScope.Mine, response.Scope);
        Assert.AreEqual(_fromDate, response.FromDate);
        Assert.AreEqual(_toDate, response.ToDate);
        Assert.AreEqual(ReportStatus.Pending, response.Status);
        Assert.AreEqual(TestData.Now.UtcDateTime, response.CreatedAt);
        Assert.IsNull(response.StartedAt);
        Assert.IsNull(response.CompletedAt);
        Assert.IsNull(response.Result);
        Assert.IsNull(response.Error);

        Assert.HasCount(1, repo.SavedJobs);
        Assert.AreEqual(TestUsers.Employee.Id, repo.SavedJobs[0].OwnerId);
        Assert.HasCount(1, publisher.PublishedIds);
        Assert.AreEqual(response.Id, publisher.PublishedIds[0]);
    }

    [TestMethod]
    public async Task RequestAsync_FromDateAfterToDate_ReturnsValidationAndDoesNotPublish()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();

        CreateReportRequest request = new()
        {
            FromDate = new DateOnly(2026, 9, 30),
            ToDate = new DateOnly(2026, 9, 1),
            Scope = ReportScope.Mine,
        };

        ServiceError error = ResultAssert.Failed(
            await service.RequestAsync(request, TestUsers.Employee, CancellationToken.None),
            ServiceErrorKind.Validation);

        Assert.AreEqual(nameof(CreateReportRequest.FromDate), error.Field);
        Assert.IsEmpty(repo.SavedJobs);
        Assert.IsEmpty(publisher.PublishedIds);
    }

    [TestMethod]
    public async Task RequestAsync_EmployeeRequestsGlobal_ReturnsForbiddenAndDoesNotPublish()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();

        CreateReportRequest request = new()
        {
            FromDate = _fromDate,
            ToDate = _toDate,
            Scope = ReportScope.Global,
        };

        ResultAssert.Failed(
            await service.RequestAsync(request, TestUsers.Employee, CancellationToken.None),
            ServiceErrorKind.Forbidden);

        Assert.IsEmpty(repo.SavedJobs);
        Assert.IsEmpty(publisher.PublishedIds);
    }

    [TestMethod]
    public async Task RequestAsync_AuditorRequestsMineWithoutEmployeeRole_ReturnsForbidden()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();

        CreateReportRequest request = new()
        {
            FromDate = _fromDate,
            ToDate = _toDate,
            Scope = ReportScope.Mine,
        };

        ResultAssert.Failed(
            await service.RequestAsync(request, TestUsers.Auditor, CancellationToken.None),
            ServiceErrorKind.Forbidden);

        Assert.IsEmpty(repo.SavedJobs);
        Assert.IsEmpty(publisher.PublishedIds);
    }

    [TestMethod]
    public async Task RequestAsync_AuditorRequestsGlobal_Succeeds()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();

        CreateReportRequest request = new()
        {
            FromDate = _fromDate,
            ToDate = _toDate,
            Scope = ReportScope.Global,
        };

        ReportResponse response = ResultAssert.Succeeded(
            await service.RequestAsync(request, TestUsers.Auditor, CancellationToken.None));

        Assert.AreEqual(ReportScope.Global, response.Scope);
        Assert.HasCount(1, repo.SavedJobs);
        Assert.HasCount(1, publisher.PublishedIds);
    }

    [TestMethod]
    public async Task RequestAsync_EmployeeAndAuditor_CanRequestBothMineAndGlobal()
    {
        (ReportService service, _, FakeReportMessagePublisher publisher, _) = CreateSut();

        ReportResponse mine = ResultAssert.Succeeded(
            await service.RequestAsync(
                new CreateReportRequest { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Mine },
                TestUsers.EmployeeAndAuditor,
                CancellationToken.None));

        ReportResponse global = ResultAssert.Succeeded(
            await service.RequestAsync(
                new CreateReportRequest { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Global },
                TestUsers.EmployeeAndAuditor,
                CancellationToken.None));

        Assert.AreEqual(ReportScope.Mine, mine.Scope);
        Assert.AreEqual(ReportScope.Global, global.Scope);
        Assert.HasCount(2, publisher.PublishedIds);
    }

    [TestMethod]
    public async Task RequestAsync_AdminApproverOrFinanceWithoutReportRoles_ReturnsForbidden()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();

        CreateReportRequest mineRequest = new() { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Mine };
        CreateReportRequest globalRequest = new() { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Global };

        ResultAssert.Failed(await service.RequestAsync(mineRequest, TestUsers.Admin, CancellationToken.None), ServiceErrorKind.Forbidden);
        ResultAssert.Failed(await service.RequestAsync(globalRequest, TestUsers.Admin, CancellationToken.None), ServiceErrorKind.Forbidden);
        ResultAssert.Failed(await service.RequestAsync(mineRequest, TestUsers.Approver, CancellationToken.None), ServiceErrorKind.Forbidden);
        ResultAssert.Failed(await service.RequestAsync(mineRequest, TestUsers.Finance, CancellationToken.None), ServiceErrorKind.Forbidden);

        Assert.IsEmpty(repo.SavedJobs);
        Assert.IsEmpty(publisher.PublishedIds);
    }

    [TestMethod]
    public async Task RequestAsync_BrokerPublishUnconfirmed_PreservesPendingJobAndReturnsServiceUnavailableWithId()
    {
        (ReportService service, InMemoryReportRepository repo, FakeReportMessagePublisher publisher, _) = CreateSut();
        publisher.ConfirmNextPublish = false;

        CreateReportRequest request = new()
        {
            FromDate = _fromDate,
            ToDate = _toDate,
            Scope = ReportScope.Mine,
        };

        ServiceError error = ResultAssert.Failed(
            await service.RequestAsync(request, TestUsers.Employee, CancellationToken.None),
            ServiceErrorKind.ServiceUnavailable);

        Assert.HasCount(1, repo.SavedJobs);
        Assert.AreEqual(repo.SavedJobs[0].Id, error.ResourceId);
        Assert.AreEqual(ReportStatus.Pending, repo.SavedJobs[0].Status);
    }

    [TestMethod]
    public async Task GetAndListAsync_OtherUserOrAuditor_CannotReadForeignReport()
    {
        (ReportService service, _, _, _) = CreateSut();

        ReportResponse created = ResultAssert.Succeeded(
            await service.RequestAsync(
                new CreateReportRequest { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Mine },
                TestUsers.Employee,
                CancellationToken.None));

        ResultAssert.Failed(
            await service.GetAsync(created.Id, TestUsers.OtherEmployee, CancellationToken.None),
            ServiceErrorKind.NotFound);

        ResultAssert.Failed(
            await service.GetAsync(created.Id, TestUsers.Auditor, CancellationToken.None),
            ServiceErrorKind.NotFound);

        IReadOnlyList<ReportResponse> auditorList = ResultAssert.Succeeded(
            await service.ListAsync(TestUsers.Auditor, CancellationToken.None));
        Assert.IsEmpty(auditorList);

        IReadOnlyList<ReportResponse> ownerList = ResultAssert.Succeeded(
            await service.ListAsync(TestUsers.Employee, CancellationToken.None));
        Assert.HasCount(1, ownerList);
        Assert.AreEqual(created.Id, ownerList[0].Id);
    }

    [TestMethod]
    public async Task DownloadExcelAsync_CompletedReport_ReturnsValidSpreadsheetMatchingPersistedResult()
    {
        (ReportService service, InMemoryReportRepository repo, _, FixedTimeProvider clock) = CreateSut();
        ReportProcessor processor = new(repo, clock, NullLogger<ReportProcessor>.Instance);

        SeedVerifiableExpenses(repo);

        ReportResponse created = ResultAssert.Succeeded(
            await service.RequestAsync(
                new CreateReportRequest { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Mine },
                TestUsers.Employee,
                CancellationToken.None));

        clock.Advance(TimeSpan.FromSeconds(2));
        ReportProcessingOutcome outcome = await processor.ProcessAsync(created.Id, CancellationToken.None);
        Assert.AreEqual(ReportProcessingOutcome.Completed, outcome);

        ReportExcelFile excel = ResultAssert.Succeeded(
            await service.DownloadExcelAsync(created.Id, TestUsers.Employee, CancellationToken.None));

        Assert.AreEqual($"report-{created.Id}.xlsx", excel.FileName);
        Assert.AreEqual(ReportExcelGenerator.ExcelContentType, excel.ContentType);
        Assert.IsNotEmpty(excel.Content);

        using MemoryStream stream = new(excel.Content);
        using XLWorkbook workbook = new(stream);
        IXLWorksheet sheet = workbook.Worksheet("Relatório");

        Assert.AreEqual(created.Id.ToString(), sheet.Cell(1, 2).GetString());
        Assert.AreEqual("Mine", sheet.Cell(2, 2).GetString());
        Assert.AreEqual("2026-09-01", sheet.Cell(3, 2).GetString());
        Assert.AreEqual("2026-09-30", sheet.Cell(4, 2).GetString());

        Assert.AreEqual("Draft", sheet.Cell(8, 1).GetString());
        Assert.AreEqual(0, sheet.Cell(8, 2).GetValue<int>());
        Assert.AreEqual(0.00m, sheet.Cell(8, 3).GetValue<decimal>());

        Assert.AreEqual("Submitted", sheet.Cell(9, 1).GetString());
        Assert.AreEqual(0, sheet.Cell(9, 2).GetValue<int>());
        Assert.AreEqual(0.00m, sheet.Cell(9, 3).GetValue<decimal>());

        Assert.AreEqual("Approved", sheet.Cell(10, 1).GetString());
        Assert.AreEqual(2, sheet.Cell(10, 2).GetValue<int>());
        Assert.AreEqual(150.00m, sheet.Cell(10, 3).GetValue<decimal>());

        Assert.AreEqual("Rejected", sheet.Cell(11, 1).GetString());
        Assert.AreEqual(0, sheet.Cell(11, 2).GetValue<int>());
        Assert.AreEqual(0.00m, sheet.Cell(11, 3).GetValue<decimal>());

        Assert.AreEqual("Paid", sheet.Cell(12, 1).GetString());
        Assert.AreEqual(1, sheet.Cell(12, 2).GetValue<int>());
        Assert.AreEqual(25.00m, sheet.Cell(12, 3).GetValue<decimal>());

        Assert.AreEqual("Total", sheet.Cell(13, 1).GetString());
        Assert.AreEqual(3, sheet.Cell(13, 2).GetValue<int>());
        Assert.AreEqual(175.00m, sheet.Cell(13, 3).GetValue<decimal>());
    }

    [TestMethod]
    public async Task DownloadExcelAsync_PendingFailedOrForeignReport_Returns409ForOwnAnd404ForForeign()
    {
        (ReportService service, InMemoryReportRepository repo, _, FixedTimeProvider clock) = CreateSut();

        ReportResponse pending = ResultAssert.Succeeded(
            await service.RequestAsync(
                new CreateReportRequest { FromDate = _fromDate, ToDate = _toDate, Scope = ReportScope.Mine },
                TestUsers.Employee,
                CancellationToken.None));

        ResultAssert.Failed(
            await service.DownloadExcelAsync(pending.Id, TestUsers.Employee, CancellationToken.None),
            ServiceErrorKind.Conflict);

        ResultAssert.Failed(
            await service.DownloadExcelAsync(pending.Id, TestUsers.OtherEmployee, CancellationToken.None),
            ServiceErrorKind.NotFound);

        ResultAssert.Failed(
            await service.DownloadExcelAsync(pending.Id, TestUsers.Auditor, CancellationToken.None),
            ServiceErrorKind.NotFound);

        ReportJob job = repo.SavedJobs.Single(item => item.Id == pending.Id);
        job.Fail("SimulatedFailure", "Erro definitivo simulado.", clock.GetUtcNow().UtcDateTime);

        ResultAssert.Failed(
            await service.DownloadExcelAsync(pending.Id, TestUsers.Employee, CancellationToken.None),
            ServiceErrorKind.Conflict);
    }

    private static (
        ReportService Service,
        InMemoryReportRepository Repository,
        FakeReportMessagePublisher Publisher,
        FixedTimeProvider Clock) CreateSut()
    {
        InMemoryReportRepository repository = new();
        FakeReportMessagePublisher publisher = new();
        FixedTimeProvider clock = new(TestData.Now);
        ReportService service = new(repository, publisher, clock, NullLogger<ReportService>.Instance);
        return (service, repository, publisher, clock);
    }

    private static void SeedVerifiableExpenses(InMemoryReportRepository repo)
    {
        ExpenseCategory category = new(1, "Transporte");
        DateTime created = TestData.Now.UtcDateTime.AddDays(-10);

        Expense e1 = Expense.CreateDraft(TestUsers.Employee.Id, category, "Despesa aprovada 1 no período", 100.00m, new DateOnly(2026, 9, 1), created);
        e1.Submit(TestUsers.Employee.Id, created);
        e1.Approve(TestUsers.Approver.Id, created);

        Expense e2 = Expense.CreateDraft(TestUsers.Employee.Id, category, "Despesa aprovada 2 no período", 50.00m, new DateOnly(2026, 9, 30), created);
        e2.Submit(TestUsers.Employee.Id, created);
        e2.Approve(TestUsers.Approver.Id, created);

        Expense e3 = Expense.CreateDraft(TestUsers.Employee.Id, category, "Despesa paga no período", 25.00m, new DateOnly(2026, 9, 15), created);
        e3.Submit(TestUsers.Employee.Id, created);
        e3.Approve(TestUsers.Approver.Id, created);
        e3.Pay(TestUsers.Finance.Id, created);

        Expense e4 = Expense.CreateDraft(TestUsers.OtherEmployee.Id, category, "Despesa de outro funcionário", 200.00m, new DateOnly(2026, 9, 10), created);
        e4.Submit(TestUsers.OtherEmployee.Id, created);
        e4.Approve(TestUsers.Approver.Id, created);

        Expense e5 = Expense.CreateDraft(TestUsers.Employee.Id, category, "Despesa fora do período", 500.00m, new DateOnly(2026, 8, 31), created);
        e5.Submit(TestUsers.Employee.Id, created);
        e5.Approve(TestUsers.Approver.Id, created);
        e5.Pay(TestUsers.Finance.Id, created);

        repo.SeedExpense(e1);
        repo.SeedExpense(e2);
        repo.SeedExpense(e3);
        repo.SeedExpense(e4);
        repo.SeedExpense(e5);
    }
}
