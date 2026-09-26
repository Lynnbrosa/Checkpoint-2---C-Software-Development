using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace ExpenseHub.Api.Data;

internal sealed class EfExpenseRepository : IExpenseRepository
{
    private readonly ExpenseHubDbContext _context;

    public EfExpenseRepository(ExpenseHubDbContext context)
    {
        _context = context;
    }

    public Task<ExpenseCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken) =>
        _context.ExpenseCategories.FirstOrDefaultAsync(category => category.Id == categoryId, cancellationToken);

    public void Add(Expense expense) => _context.Expenses.Add(expense);

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
