using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Services.Users;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class UserAdministrationServiceTests
{
    private const string AnaId = "ana";

    private FakeUserDirectory _directory = null!;
    private UserAdministrationService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = new FakeUserDirectory();
        _directory.Add(TestUsers.Admin.Id, "admin@empresa.com", Roles.Admin);
        _directory.Add(AnaId, "ana@empresa.com");
        _service = new UserAdministrationService(_directory);
    }

    [TestMethod]
    public async Task ListAsync_AsAdmin_ReturnsUsersWithRoles()
    {
        ServiceResult<IReadOnlyList<UserResponse>> result = await _service.ListAsync(TestUsers.Admin, CancellationToken.None);

        IReadOnlyList<UserResponse> users = ResultAssert.Succeeded(result);
        Assert.HasCount(2, users);
        CollectionAssert.AreEqual(new[] { Roles.Admin }, users.Single(user => user.Id == TestUsers.Admin.Id).Roles.ToArray());
    }

    [TestMethod]
    [DataRow(Roles.Employee)]
    [DataRow(Roles.Approver)]
    [DataRow(Roles.Finance)]
    [DataRow(Roles.Auditor)]
    public async Task ListAsync_WithoutAdminRole_ReturnsForbidden(string role)
    {
        ServiceResult<IReadOnlyList<UserResponse>> result = await _service.ListAsync(
            new UserContext("user-" + role, [role]),
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
    }

    [TestMethod]
    public async Task UpdateRolesAsync_AddsAndRemovesToMatchTheRequest()
    {
        _directory.Add("bia", "bia@empresa.com", Roles.Employee, Roles.Auditor);

        ServiceResult<UserResponse> result = await UpdateAsync("bia", Roles.Employee, Roles.Approver);

        CollectionAssert.AreEquivalent(new[] { Roles.Employee, Roles.Approver }, ResultAssert.Succeeded(result).Roles.ToArray());
        CollectionAssert.AreEquivalent(new[] { Roles.Employee, Roles.Approver }, _directory.RolesOf("bia").ToArray());
    }

    [TestMethod]
    public async Task UpdateRolesAsync_EmptyList_RemovesEveryRole()
    {
        _directory.Add("bia", "bia@empresa.com", Roles.Employee);

        ServiceResult<UserResponse> result = await UpdateAsync("bia");

        Assert.IsEmpty(ResultAssert.Succeeded(result).Roles);
    }

    [TestMethod]
    [DataRow("Chefe")]
    [DataRow("employee")]
    [DataRow("")]
    public async Task UpdateRolesAsync_UnknownRole_ReturnsValidationAndChangesNothing(string role)
    {
        ServiceResult<UserResponse> result = await UpdateAsync(AnaId, Roles.Employee, role);

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("Roles", error.Field);
        Assert.IsEmpty(_directory.RolesOf(AnaId));
        Assert.AreEqual(0, _directory.RoleChanges);
    }

    [TestMethod]
    public async Task UpdateRolesAsync_UnknownUser_ReturnsNotFound()
    {
        ServiceResult<UserResponse> result = await UpdateAsync("nao-existe", Roles.Employee);

        ResultAssert.Failed(result, ServiceErrorKind.NotFound);
    }

    [TestMethod]
    public async Task UpdateRolesAsync_AdminRemovingOwnAdminRole_ReturnsForbidden()
    {
        ServiceResult<UserResponse> result = await UpdateAsync(TestUsers.Admin.Id, Roles.Employee);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        CollectionAssert.AreEqual(new[] { Roles.Admin }, _directory.RolesOf(TestUsers.Admin.Id).ToArray());
    }

    [TestMethod]
    public async Task UpdateRolesAsync_AdminKeepingAdminWhileAddingEmployee_IsAllowed()
    {
        ServiceResult<UserResponse> result = await UpdateAsync(TestUsers.Admin.Id, Roles.Admin, Roles.Employee);

        CollectionAssert.AreEquivalent(new[] { Roles.Admin, Roles.Employee }, ResultAssert.Succeeded(result).Roles.ToArray());
    }

    [TestMethod]
    public async Task UpdateRolesAsync_AdminCanRemoveAdminFromAnotherAdmin()
    {
        _directory.Add("outro-admin", "outro@empresa.com", Roles.Admin);

        ServiceResult<UserResponse> result = await UpdateAsync("outro-admin", Roles.Employee);

        CollectionAssert.AreEqual(new[] { Roles.Employee }, ResultAssert.Succeeded(result).Roles.ToArray());
    }

    [TestMethod]
    public async Task UpdateRolesAsync_SameRoles_DoesNotTouchTheDirectory()
    {
        _directory.Add("bia", "bia@empresa.com", Roles.Employee);

        await UpdateAsync("bia", Roles.Employee);

        // sem troca de role não há stamp novo, então o token atual continua valendo
        Assert.AreEqual(0, _directory.RoleChanges);
    }

    [TestMethod]
    public async Task UpdateRolesAsync_DuplicatedRolesInRequest_AreTreatedAsOne()
    {
        ServiceResult<UserResponse> result = await UpdateAsync(AnaId, Roles.Employee, Roles.Employee);

        CollectionAssert.AreEqual(new[] { Roles.Employee }, ResultAssert.Succeeded(result).Roles.ToArray());
    }

    [TestMethod]
    public async Task UpdateRolesAsync_WithoutAdminRole_ReturnsForbidden()
    {
        ServiceResult<UserResponse> result = await _service.UpdateRolesAsync(
            AnaId,
            new UpdateUserRolesRequest { Roles = [Roles.Admin] },
            TestUsers.Employee,
            CancellationToken.None);

        ResultAssert.Failed(result, ServiceErrorKind.Forbidden);
        Assert.IsEmpty(_directory.RolesOf(AnaId));
    }

    private Task<ServiceResult<UserResponse>> UpdateAsync(string userId, params string[] roles) =>
        _service.UpdateRolesAsync(userId, new UpdateUserRolesRequest { Roles = roles }, TestUsers.Admin, CancellationToken.None);
}
