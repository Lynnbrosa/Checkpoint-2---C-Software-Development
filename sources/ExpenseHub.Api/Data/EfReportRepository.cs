using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services.Reports;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

internal sealed class EfReportRepository : IReportRepository
{
    private readonly ExpenseHubDbContext _context;

    public EfReportRepository(ExpenseHubDbContext context)
    {
        _context = context;
    }

    public void Add(ReportJob job) => _context.ReportJobs.Add(job);

    public Task<ReportJob?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ReportJobs.FirstOrDefaultAsync(job => job.Id == id, cancellationToken);

    public Task<ReportJob?> FindOwnedByIdAsync(Guid id, string ownerId, CancellationToken cancellationToken) =>
        _context.ReportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == id && job.OwnerId == ownerId, cancellationToken);

    public async Task<IReadOnlyList<ReportJob>> ListByOwnerAsync(string ownerId, CancellationToken cancellationToken) =>
        await _context.ReportJobs
            .AsNoTracking()
            .Where(job => job.OwnerId == ownerId)
            .OrderByDescending(job => job.CreatedAt)
            .ThenBy(job => job.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Expense>> ListEligibleExpensesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        ReportScope scope,
        string ownerId,
        CancellationToken cancellationToken)
    {
        IQueryable<Expense> query = _context.Expenses
            .AsNoTracking()
            .Where(expense => expense.ExpenseDate >= fromDate && expense.ExpenseDate <= toDate);

        if (scope == ReportScope.Mine)
        {
            query = query.Where(expense => expense.OwnerId == ownerId);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}

