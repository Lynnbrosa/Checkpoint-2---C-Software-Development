using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;

namespace ExpenseHub.UnitTests;

internal static class TestUsers
{
    public static UserContext Employee { get; } = new("employee-1", [Roles.Employee]);

    public static UserContext OtherEmployee { get; } = new("employee-2", [Roles.Employee]);

    public static UserContext Approver { get; } = new("approver-1", [Roles.Approver]);

    public static UserContext Finance { get; } = new("finance-1", [Roles.Finance]);

    public static UserContext Auditor { get; } = new("auditor-1", [Roles.Auditor]);

    public static UserContext Admin { get; } = new("admin-1", [Roles.Admin]);

    public static UserContext WithoutRoles { get; } = new("nobody-1", []);

    public static UserContext EmployeeAndApprover { get; } = new("employee-approver-1", [Roles.Employee, Roles.Approver]);

    public static UserContext EmployeeAndFinance { get; } = new("employee-finance-1", [Roles.Employee, Roles.Finance]);

    public static UserContext AdminAndEmployee { get; } = new("admin-employee-1", [Roles.Admin, Roles.Employee]);
}
