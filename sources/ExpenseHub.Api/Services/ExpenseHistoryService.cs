using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal sealed class ExpenseHistoryService : IExpenseHistoryService
{
    private readonly IExpenseRepository _repository;

    public ExpenseHistoryService(IExpenseRepository repository)
    {
        _repository = repository;
    }

    public async Task<ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>> GetHistoryAsync(
        Guid expenseId,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!ExpenseAccessPolicy.HasReadRole(user))
        {
            return new ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>(
                ServiceError.Forbidden("O perfil não consulta histórico."));
        }

        // mesma regra do detalhe: se não enxerga o reembolso, o histórico dele também não existe
        Expense? expense = await _repository.FindAsync(expenseId, ExpenseAccessPolicy.ReadableBy(user), cancellationToken);
        if (expense is null)
        {
            return new ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>(
                ServiceError.NotFound("Reembolso não encontrado."));
        }

        IReadOnlyList<ExpenseHistory> entries = await _repository.ListHistoryAsync(expense.Id, cancellationToken);
        return new ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>(entries.Select(ToResponse).ToList());
    }

    private static ExpenseHistoryResponse ToResponse(ExpenseHistory entry) => new()
    {
        Action = entry.Action,
        ActorId = entry.ActorId,
        OccurredAt = entry.OccurredAt,
        FromStatus = entry.FromStatus,
        ToStatus = entry.ToStatus,
        Justification = entry.Justification,
        Changes = ExpenseChangeLog.Deserialize(entry.Changes)
            .Select(change => new FieldChangeResponse { Field = change.Field, From = change.From, To = change.To })
            .ToList(),
    };
}
