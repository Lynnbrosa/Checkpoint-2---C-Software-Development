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
internal sealed class ExpenseOwnershipTests
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
    public async Task UpdateAsync_ApproverThatIsAlsoEmployee_CannotEditSubmittedOfOthers()
    {
        Expense submitted = TestData.Submitted(TestUsers.Employee);
        _repository.Seed(submitted);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            submitted.Id,
            TestData.DraftRequest(),
            TestUsers.EmployeeAndApprover,
            CancellationToken.None);

        // enxerga pelo Approver, mas não é dono: 403 e não 404
        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task UpdateAsync_AuditorThatIsAlsoEmployee_CannotEditDraftOfOthers()
    {
        UserContext auditorEmployee = new("auditor-employee-1", [Roles.Auditor, Roles.Employee]);
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.UpdateAsync(
            draft.Id,
            TestData.DraftRequest(description: "Auditor tentando escrever"),
            auditorEmployee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.AreEqual("Almoço com cliente em São Paulo", draft.Description);
    }

    [TestMethod]
    public async Task SubmitAsync_ApproverThatIsAlsoEmployee_CannotSubmitDraftOfOthers()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.SubmitAsync(draft.Id, TestUsers.EmployeeAndApprover, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
        Assert.AreEqual(ExpenseStatus.Draft, draft.Status);
    }

    [TestMethod]
    public async Task CreateAsync_AdminWithEmployeeRole_WorksAsEmployee()
    {
        ServiceResult<ExpenseResponse> result = await _service.CreateAsync(
            TestData.DraftRequest(),
            TestUsers.AdminAndEmployee,
            CancellationToken.None);

        Assert.AreEqual(TestUsers.AdminAndEmployee.Id, ResultAssert.Succeeded(result).OwnerId);
    }

    [TestMethod]
    public async Task GetAsync_AdminAlone_ReturnsForbidden()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.GetAsync(draft.Id, TestUsers.Admin, CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task GetAsync_AuditorSeesDraftsOfAnyone()
    {
        Expense draft = TestData.Draft(TestUsers.Employee);
        _repository.Seed(draft);

        ServiceResult<ExpenseResponse> result = await _service.GetAsync(draft.Id, TestUsers.Auditor, CancellationToken.None);

        Assert.AreEqual(draft.Id, ResultAssert.Succeeded(result).Id);
    }
}
