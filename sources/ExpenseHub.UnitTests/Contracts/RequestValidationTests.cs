using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ExpenseHub.Api.Contracts.Requests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpenseHub.UnitTests.Contracts;

// as mesmas regras que o [ApiController] aplica antes de chegar no serviço
[TestClass]
internal sealed class RequestValidationTests
{
    private static readonly string[] _editableDraftFields = ["Description", "Amount", "ExpenseDate", "CategoryId"];

    [TestMethod]
    public void ExpenseDraftRequest_Valid_HasNoErrors()
    {
        Assert.IsEmpty(Validate(TestData.DraftRequest()));
    }

    [TestMethod]
    [DataRow("0")]
    [DataRow("0.001")]
    [DataRow("-5")]
    [DataRow("2147483647.01")]
    public void ExpenseDraftRequest_AmountOutOfRange_FailsOnAmount(string amount)
    {
        ExpenseDraftRequest request = TestData.DraftRequest(amount: decimal.Parse(amount, CultureInfo.InvariantCulture));

        Assert.IsTrue(FailsOn(Validate(request), nameof(ExpenseDraftRequest.Amount)));
    }

    [TestMethod]
    [DataRow("0.01")]
    [DataRow("2147483647")]
    public void ExpenseDraftRequest_AmountOnTheLimits_IsValid(string amount)
    {
        ExpenseDraftRequest request = TestData.DraftRequest(amount: decimal.Parse(amount, CultureInfo.InvariantCulture));

        Assert.IsEmpty(Validate(request));
    }

    [TestMethod]
    public void ExpenseDraftRequest_AmountRange_DoesNotDependOnTheMachineCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            // em pt-BR "0.01" viraria 1 se o limite fosse lido com a cultura atual
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");

            Assert.IsEmpty(Validate(TestData.DraftRequest(amount: 0.01m)));
            Assert.IsTrue(FailsOn(Validate(TestData.DraftRequest(amount: 2147483647.01m)), nameof(ExpenseDraftRequest.Amount)));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(501)]
    public void ExpenseDraftRequest_DescriptionOutOfBounds_FailsOnDescription(int length)
    {
        ExpenseDraftRequest request = TestData.DraftRequest(description: new string('a', length));

        Assert.IsTrue(FailsOn(Validate(request), nameof(ExpenseDraftRequest.Description)));
    }

    [TestMethod]
    public void ExpenseDraftRequest_BlankDescription_FailsOnDescription()
    {
        ExpenseDraftRequest request = TestData.DraftRequest(description: new string(' ', 30));

        Assert.IsTrue(FailsOn(Validate(request), nameof(ExpenseDraftRequest.Description)));
    }

    [TestMethod]
    public void ExpenseDraftRequest_CategoryZero_FailsOnCategory()
    {
        ExpenseDraftRequest request = TestData.DraftRequest(categoryId: 0);

        Assert.IsTrue(FailsOn(Validate(request), nameof(ExpenseDraftRequest.CategoryId)));
    }

    [TestMethod]
    public void ExpenseDraftRequest_OnlyAcceptsTheEditableFields()
    {
        // se alguém adicionar OwnerId, Status ou datas de servidor no DTO, esse teste acusa
        string[] properties = PublicPropertiesOf<ExpenseDraftRequest>();

        CollectionAssert.AreEquivalent(_editableDraftFields, properties);
    }

    [TestMethod]
    public void RegisterRequest_HasNoRoleField()
    {
        string[] properties = PublicPropertiesOf<RegisterRequest>();

        Assert.IsFalse(properties.Any(name => name.Contains("Role", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    [DataRow(9)]
    [DataRow(501)]
    public void RejectExpenseRequest_JustificationOutOfBounds_Fails(int length)
    {
        RejectExpenseRequest request = new() { Justification = new string('x', length) };

        Assert.IsTrue(FailsOn(Validate(request), nameof(RejectExpenseRequest.Justification)));
    }

    [TestMethod]
    public void RejectExpenseRequest_BlankJustification_Fails()
    {
        RejectExpenseRequest request = new() { Justification = "            " };

        Assert.IsTrue(FailsOn(Validate(request), nameof(RejectExpenseRequest.Justification)));
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(500)]
    public void RejectExpenseRequest_JustificationOnTheLimits_IsValid(int length)
    {
        Assert.IsEmpty(Validate(new RejectExpenseRequest { Justification = new string('x', length) }));
    }

    [TestMethod]
    public void RegisterRequest_InvalidEmail_FailsOnEmail()
    {
        RegisterRequest request = new() { Email = "nao-e-email", Password = TestSecrets.UserSecret };

        Assert.IsTrue(FailsOn(Validate(request), nameof(RegisterRequest.Email)));
    }

    [TestMethod]
    public void RegisterRequest_MissingPassword_FailsOnPassword()
    {
        RegisterRequest request = new() { Email = "ana@empresa.com", Password = string.Empty };

        Assert.IsTrue(FailsOn(Validate(request), nameof(RegisterRequest.Password)));
    }

    [TestMethod]
    public void RegisterRequest_NameTooLong_FailsOnFullName()
    {
        RegisterRequest request = new()
        {
            Email = "ana@empresa.com",
            Password = TestSecrets.UserSecret,
            FullName = new string('a', 121),
        };

        Assert.IsTrue(FailsOn(Validate(request), nameof(RegisterRequest.FullName)));
    }

    [TestMethod]
    public void UpdateUserRolesRequest_NullRoles_Fails()
    {
        UpdateUserRolesRequest request = new() { Roles = null! };

        Assert.IsTrue(FailsOn(Validate(request), nameof(UpdateUserRolesRequest.Roles)));
    }

    [TestMethod]
    public void UpdateUserRolesRequest_EmptyList_IsValid()
    {
        Assert.IsEmpty(Validate(new UpdateUserRolesRequest { Roles = [] }));
    }

    [TestMethod]
    public void LoginRequest_InvalidEmail_FailsOnEmail()
    {
        LoginRequest request = new() { Email = "ana", Password = TestSecrets.UserSecret };

        Assert.IsTrue(FailsOn(Validate(request), nameof(LoginRequest.Email)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static bool FailsOn(List<ValidationResult> results, string member) =>
        results.Any(result => result.MemberNames.Contains(member));

    private static string[] PublicPropertiesOf<T>() =>
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.Name).ToArray();
}
