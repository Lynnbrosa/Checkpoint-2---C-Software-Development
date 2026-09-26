using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Services;

namespace ExpenseHub.UnitTests.Fakes;

// imita o contrato do repositório do ef: Add só vale depois do save, e o scope filtra antes de devolver
internal sealed class InMemoryExpenseRepository : IExpenseRepository
{
    private readonly List<Expense> _saved = [];
    private readonly List<Expense> _pending = [];
    private readonly Dictionary<int, ExpenseCategory> _categories = new()
    {
        [1] = new ExpenseCategory(1, "Transporte"),
        [2] = new ExpenseCategory(2, "Alimentação"),
    };

    public IReadOnlyList<Expense> Saved => _saved;

    public int SaveCount { get; private set; }

    // simula outra requisição ter alterado o reembolso antes do save
    public bool FailNextSave { get; set; }

    public void Seed(Expense expense) => _saved.Add(expense);

    public ExpenseCategory Category(int id) => _categories[id];

    public Task<ExpenseCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken) =>
        Task.FromResult(_categories.GetValueOrDefault(categoryId));

    public Task<Expense?> FindAsync(Guid id, Expression<Func<Expense, bool>> scope, CancellationToken cancellationToken) =>
        Task.FromResult(_saved.AsQueryable().Where(scope).FirstOrDefault(expense => expense.Id == id));

    public void Add(Expense expense) => _pending.Add(expense);

    public Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken)
    {
        if (FailNextSave)
        {
            FailNextSave = false;
            _pending.Clear();
            return Task.FromResult(false);
        }

        _saved.AddRange(_pending);
        _pending.Clear();
        SaveCount++;
        return Task.FromResult(true);
    }
}
