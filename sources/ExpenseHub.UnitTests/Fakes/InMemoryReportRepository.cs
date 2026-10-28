using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services.Reports;

namespace ExpenseHub.UnitTests.Fakes;

internal sealed class InMemoryReportRepository : IReportRepository
{
    private readonly List<ReportJob> _savedJobs = [];
    private readonly List<ReportJob> _pendingJobs = [];
    private readonly List<Expense> _expenses = [];

    public IReadOnlyList<ReportJob> SavedJobs => _savedJobs;

    public int SaveCount { get; private set; }

    public int EligibleQueryCount { get; private set; }

    public bool FailNextSave { get; set; }

    public void SeedExpense(Expense expense) => _expenses.Add(expense);

    public void SeedJob(ReportJob job) => _savedJobs.Add(job);

    public void Add(ReportJob job) => _pendingJobs.Add(job);

    public Task<ReportJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_savedJobs.FirstOrDefault(job => job.Id == id));

    public Task<ReportJob?> FindOwnedByIdAsync(Guid id, string ownerId, CancellationToken cancellationToken) =>
        Task.FromResult(
            _savedJobs.FirstOrDefault(job => job.Id == id && string.Equals(job.OwnerId, ownerId, StringComparison.Ordinal)));

    public Task<IReadOnlyList<ReportJob>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportJob>>(
            _savedJobs
                .Where(job => string.Equals(job.OwnerId, ownerId, StringComparison.Ordinal))
                .OrderByDescending(job => job.CreatedAt)
                .ThenBy(job => job.Id)
                .ToList());

    public Task<IReadOnlyList<Expense>> ListEligibleExpensesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        ReportScope scope,
        string ownerId,
        CancellationToken cancellationToken)
    {
        EligibleQueryCount++;

        IEnumerable<Expense> query = _expenses
            .Where(expense => expense.ExpenseDate >= fromDate && expense.ExpenseDate <= toDate);

        if (scope == ReportScope.Mine)
        {
            query = query.Where(expense => string.Equals(expense.OwnerId, ownerId, StringComparison.Ordinal));
        }

        return Task.FromResult<IReadOnlyList<Expense>>(query.ToList());
    }

    public Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            _pendingJobs.Clear();
            return Task.FromResult(false);
        }

        _savedJobs.AddRange(_pendingJobs);
        _pendingJobs.Clear();
        SaveCount++;
        return Task.FromResult(true);
    }
}

