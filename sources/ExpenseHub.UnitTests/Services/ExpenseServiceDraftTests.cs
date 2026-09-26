using System;
using System.Globalization;
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
internal sealed class ExpenseServiceDraftTests
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
    public async Task CreateAsync_AsEmployee_CreatesDraftOwnedByTokenUser()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(),
            TestUsers.Employee,
            CancellationToken.None);

        ExpenseResponse created = ResultAssert.Succeeded(result);
        Assert.AreEqual(ExpenseStatus.Draft, created.Status);
        Assert.AreEqual(TestUsers.Employee.Id, created.OwnerId);
        Assert.AreEqual(TestData.Now.UtcDateTime, created.CreatedAt);
        Assert.AreEqual("Transporte", created.CategoryName);
        Assert.HasCount(1, _repository.Saved);
    }

    [TestMethod]
    public async Task CreateAsync_RecordsCreatedEntryWithServerActorAndTime()
    {
        await _service.CreateAsync(TestData.DraftRequest(), TestUsers.Employee, CancellationToken.None);

        ExpenseHistory entry = _repository.Saved.Single().History.Single();
        Assert.AreEqual(ExpenseAction.Created, entry.Action);
        Assert.AreEqual(TestUsers.Employee.Id, entry.ActorId);
        Assert.AreEqual(TestData.Now.UtcDateTime, entry.OccurredAt);
        Assert.IsNull(entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, entry.ToStatus);
    }

    [TestMethod]
    [DataRow(Roles.Approver)]
    [DataRow(Roles.Finance)]
    [DataRow(Roles.Auditor)]
    [DataRow(Roles.Admin)]
    public async Task CreateAsync_WithoutEmployeeRole_ReturnsForbidden(string role)
    {
        UserContext user = new("user-" + role, [role]);

        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(TestData.DraftRequest(), user, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.IsEmpty(_repository.Saved);
    }

    [TestMethod]
    public async Task CreateAsync_UserWithoutRoles_ReturnsForbidden()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(),
            TestUsers.WithoutRoles,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task CreateAsync_WithFutureDate_ReturnsValidationError()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(expenseDate: TestData.Today.AddDays(1)),
            TestUsers.Employee,
            CancellationToken.None);

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("ExpenseDate", error.Field);
        Assert.IsEmpty(_repository.Saved);
    }

    [TestMethod]
    public async Task CreateAsync_WithTodayAsDate_IsAccepted()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(expenseDate: TestData.Today),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Succeeded(result);
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("0.009")]
    [DataRow("-10")]
    [DataRow("2147483647.01")]
    public async Task CreateAsync_WithAmountOutOfRange_ReturnsValidationError(string amount)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(amount: decimal.Parse(amount, CultureInfo.InvariantCulture)),
            TestUsers.Employee,
            CancellationToken.None);

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("Amount", error.Field);
    }

    [TestMethod]
    [DataRow("0.01")]
    [DataRow("2147483647")]
    public async Task CreateAsync_WithAmountOnTheLimits_IsAccepted(string amount)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(amount: decimal.Parse(amount, CultureInfo.InvariantCulture)),
            TestUsers.Employee,
            CancellationToken.None);

        Assert.AreEqual(decimal.Parse(amount, CultureInfo.InvariantCulture), ResultAssert.Succeeded(result).Amount);
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(501)]
    public async Task CreateAsync_WithDescriptionOutOfBounds_ReturnsValidationError(int length)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(description: new string('a', length)),
            TestUsers.Employee,
            CancellationToken.None);

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("Description", error.Field);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public async Task CreateAsync_WithDescriptionOnTheLimits_IsAccepted(int length)
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(description: new string('a', length)),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Succeeded(result);
    }

    [TestMethod]
    public async Task CreateAsync_WithBlankDescription_ReturnsValidationError()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(description: new string(' ', 20)),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Validation);
    }

    [TestMethod]
    public async Task CreateAsync_WithUnknownCategory_ReturnsValidationError()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(categoryId: 99),
            TestUsers.Employee,
            CancellationToken.None);

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("CategoryId", error.Field);
    }

    [TestMethod]
    public async Task UpdateAsync_OwnDraft_AppliesChangesAndRecordsThemInHistory()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            draft.Id,
            TestData.DraftRequest(description: "Almoço com cliente em Campinas", amount: 150.25m, categoryId: 2),
            TestUsers.Employee,
            CancellationToken.None);

        ExpenseResponse updated = ResultAssert.Succeeded(result);
        Assert.AreEqual("Almoço com cliente em Campinas", updated.Description);
        Assert.AreEqual(150.25m, updated.Amount);
        Assert.AreEqual(2, updated.CategoryId);
        Assert.AreEqual(ExpenseStatus.Draft, updated.Status);

        ExpenseHistory entry = draft.History.Last();
        Assert.AreEqual(ExpenseAction.Updated, entry.Action);
        Assert.AreEqual(TestUsers.Employee.Id, entry.ActorId);
        Assert.AreEqual(ExpenseStatus.Draft, entry.FromStatus);
        Assert.AreEqual(ExpenseStatus.Draft, entry.ToStatus);
        Assert.IsNotNull(entry.Changes);
        Assert.Contains("\"field\":\"description\"", entry.Changes);
        Assert.Contains("\"from\":\"120.00\",\"to\":\"150.25\"", entry.Changes);
        Assert.Contains("\"field\":\"categoryId\"", entry.Changes);
    }

    [TestMethod]
    public async Task UpdateAsync_WithSameValues_DoesNotAddHistory()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        await _service.UpdateAsync(
            draft.Id,
            TestData.DraftRequest(
                description: draft.Description,
                amount: draft.Amount,
                expenseDate: draft.ExpenseDate,
                categoryId: draft.CategoryId),
            TestUsers.Employee,
            CancellationToken.None);

        Assert.HasCount(1, draft.History);
    }

    [TestMethod]
    public async Task UpdateAsync_DraftOfAnotherEmployee_ReturnsNotFoundAndKeepsData()
    {
        Expense draft = TestData.Draft(TestUsers.OtherEmployee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            draft.Id,
            TestData.DraftRequest(description: "Tentando editar o que não é meu"),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
        Assert.AreEqual("Almoço com cliente em São Paulo", draft.Description);
        Assert.HasCount(1, draft.History);
    }

    [TestMethod]
    public async Task UpdateAsync_UnknownExpense_ReturnsNotFound()
    {
        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            Guid.NewGuid(),
            TestData.DraftRequest(),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task UpdateAsync_WithoutEmployeeRole_ReturnsForbidden()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            draft.Id,
            TestData.DraftRequest(),
            TestUsers.Auditor,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenAnotherRequestChangedTheExpense_ReturnsConflict()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);
        _repository.FailNextSave = true;

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            draft.Id,
            TestData.DraftRequest(description: "Descrição nova que não vai ser salva"),
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
    }
}
