using System;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services.Reports;

internal static class ReportAccessPolicy
{
    public static bool CanRequestScope(UserContext user, ReportScope scope)
    {
        ArgumentNullException.ThrowIfNull(user);

        return scope switch
        {
            ReportScope.Mine => user.IsInRole(Roles.Employee),
            ReportScope.Global => user.IsInRole(Roles.Auditor),
            _ => false,
        };
    }

    public static bool IsOwner(ReportJob job, UserContext user)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(user);

        return string.Equals(job.OwnerId, user.Id, StringComparison.Ordinal);
    }
}

