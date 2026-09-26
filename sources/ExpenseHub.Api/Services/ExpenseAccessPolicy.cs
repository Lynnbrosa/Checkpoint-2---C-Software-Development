using System;
using System.Linq.Expressions;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

// matriz de leitura do MATRIZ-AUTORIZACAO.md; roles somam, nunca tiram
internal static class ExpenseAccessPolicy
{
    public static bool HasReadRole(UserContext user) =>
        user.IsInRole(Roles.Employee)
        || user.IsInRole(Roles.Approver)
        || user.IsInRole(Roles.Finance)
        || user.IsInRole(Roles.Auditor);

    // vira WHERE no banco; nada é filtrado depois de carregar
    public static Expression<Func<Expense, bool>> ReadableBy(UserContext user)
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
            (ownExpenses && expense.OwnerId == userId)
            || (submitted && expense.Status == ExpenseStatus.Submitted)
            || (approvedOrPaid && (expense.Status == ExpenseStatus.Approved || expense.Status == ExpenseStatus.Paid));
    }
}
