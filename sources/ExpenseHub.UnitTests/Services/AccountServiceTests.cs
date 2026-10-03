using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Services.Users;
using ExpenseHub.UnitTests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Services;

[TestClass]
internal sealed class AccountServiceTests
{
    private FakeUserDirectory _directory = null!;
    private AccountService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = new FakeUserDirectory();
        _service = new AccountService(_directory);
    }

    [TestMethod]
    public async Task RegisterAsync_CreatesUserWithoutAnyRole()
    {
        ServiceResult<UserResponse> result = await _service.RegisterAsync(Request("ana@empresa.com", "Ana Souza"));

        UserResponse user = ResultAssert.Succeeded(result);
        Assert.AreEqual("ana@empresa.com", user.Email);
        Assert.AreEqual("Ana Souza", user.FullName);
        Assert.IsEmpty(user.Roles);
        Assert.IsEmpty(_directory.RolesOf(user.Id));
    }

    [TestMethod]
    public async Task RegisterAsync_UsesTheEmailAsLogin()
    {
        await _service.RegisterAsync(Request("ana@empresa.com"));

        Assert.IsNotNull(_directory.LastCreated);
        Assert.AreEqual("ana@empresa.com", _directory.LastCreated.UserName);
    }

    [TestMethod]
    public async Task RegisterAsync_DuplicatedEmail_ReturnsConflict()
    {
        _directory.CreateErrors = [new IdentityErrorDescriber().DuplicateEmail("ana@empresa.com")];

        ServiceResult<UserResponse> result = await _service.RegisterAsync(Request("ana@empresa.com"));

        ResultAssert.Failed(result, ServiceErrorKind.Conflict);
    }

    [TestMethod]
    public async Task RegisterAsync_WeakPassword_ReturnsValidationOnPassword()
    {
        IdentityErrorDescriber describer = new();
        _directory.CreateErrors = [describer.PasswordTooShort(6), describer.PasswordRequiresDigit()];

        ServiceResult<UserResponse> result = await _service.RegisterAsync(Request("ana@empresa.com"));

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("Password", error.Field);
    }

    [TestMethod]
    public async Task RegisterAsync_InvalidEmailForIdentity_ReturnsValidationOnEmail()
    {
        _directory.CreateErrors = [new IdentityErrorDescriber().InvalidEmail("ana@")];

        ServiceResult<UserResponse> result = await _service.RegisterAsync(Request("ana@"));

        ServiceError error = ResultAssert.Failed(result, ServiceErrorKind.Validation);
        Assert.AreEqual("Email", error.Field);
    }

    private static RegisterRequest Request(string email, string? fullName = null) => new()
    {
        Email = email,
        Password = TestSecrets.UserSecret,
        FullName = fullName,
    };
}
