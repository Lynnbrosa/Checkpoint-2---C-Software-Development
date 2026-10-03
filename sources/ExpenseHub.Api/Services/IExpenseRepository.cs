using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services;

// os serviços falam com essa interface; nos testes ela vira uma lista em memória
internal interface IExpenseRepository
{
    Task<ExpenseCategory?> FindCategoryAsync(int categoryId, CancellationToken cancellationToken);

    // scope é o filtro de acesso do usuário; vai pro WHERE antes de trazer qualquer linha
    Task<Expense?> FindAsync(Guid id, Expression<Func<Expense, bool>> scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<Expense>> ListAsync(Expression<Func<Expense, bool>> scope, CancellationToken cancellationToken);

    // chamar só depois de confirmar que o reembolso é visível pro usuário
    Task<IReadOnlyList<ExpenseHistory>> ListHistoryAsync(Guid expenseId, CancellationToken cancellationToken);

    void Add(Expense expense);

    // false quando outra requisição alterou o mesmo reembolso antes (concorrência otimista)
    Task<bool> TrySaveChangesAsync(CancellationToken cancellationToken);
}
