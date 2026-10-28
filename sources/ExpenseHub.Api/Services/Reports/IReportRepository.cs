using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services.Reports;

internal interface IReportRepository
{
    void Add(ReportJob job);

    Task<ReportJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ReportJob?> FindOwnedByIdAsync(Guid id, string ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportJob>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> ListEligibleExpensesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        ReportScope scope,
        string ownerId,
        CancellationToken cancellationToken);

    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}

