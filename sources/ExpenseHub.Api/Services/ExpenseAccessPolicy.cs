using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

// matriz do MATRIZ-AUTORIZACAO.md num lugar só; roles somam, nunca tiram
// ordem das checagens em toda escrita: role (403) -> escopo (404) -> dono (403) -> estado (409)
internal static class ExpenseAccessPolicy
{
    public static bool HasReadRole(UserContext user) =>
        user.IsInRole(Roles.Employee)
        || user.IsInRole(Roles.Approver)
        || user.IsInRole(Roles.Finance)
        || user.IsInRole(Roles.Auditor);

    public static string RequiredRole(ExpenseOperation operation) => operation switch
    {
        ExpenseOperation.Edit or ExpenseOperation.Submit => Roles.Employee,
        ExpenseOperation.Decide => Roles.Approver,
        ExpenseOperation.Pay => Roles.Finance,
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
    };

    public static Expression<Func<Expense, bool>> ReadableBy(UserContext user) => Scope(user, includeWorkflow: false);

    // approver e finance trabalham no fluxo: reembolso já enviado existe pra eles, e estado errado vira 409.
    // rascunho alheio continua 404 pra todo mundo que não é dono nem auditor
    public static Expression<Func<Expense, bool>> ActionableBy(UserContext user, ExpenseOperation operation) =>
        Scope(user, includeWorkflow: operation is ExpenseOperation.Decide or ExpenseOperation.Pay);

    // dono edita e envia; dono nunca decide nem paga, mesmo acumulando Approver ou Finance
    public static bool OwnershipAllows(Expense expense, UserContext user, ExpenseOperation operation)
    {
        ArgumentNullException.ThrowIfNull(expense);
        ArgumentNullException.ThrowIfNull(user);

        bool isOwner = string.Equals(expense.OwnerId, user.Id, StringComparison.Ordinal);
        return operation is ExpenseOperation.Edit or ExpenseOperation.Submit ? isOwner : !isOwner;
    }

    // vira WHERE no banco; nada é filtrado depois de carregar
    private static Expression<Func<Expense, bool>> Scope(UserContext user, bool includeWorkflow)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsInRole(Roles.Auditor))
        {
            return expense => true;
        }

        string userId = user.Id;
        bool ownExpenses = user.IsInRole(Roles.Employee);
        bool submitted = user.IsInRole(Roles.Approver);
        bool approvedOrPaid = user.IsInRole(Roles.Finance);

        return expense =>
            (includeWorkflow && expense.Status != ExpenseStatus.Draft)
            || (ownExpenses && expense.OwnerId == userId)
            || (submitted && expense.Status == ExpenseStatus.Submitted)
            || (approvedOrPaid && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }
}
