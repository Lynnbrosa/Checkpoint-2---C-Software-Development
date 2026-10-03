using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal sealed class ExpensePaymentService : IExpensePaymentService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpensePaymentService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<ExpenseResponse>> PayAsync(Guid id, UserContext user, CancellationToken cancellationToken)
    {
        ServiceResult<Expense> loaded = await _repository.LoadForAsync(id, user, ExpenseOperation.Pay, cancellationToken);
        if (!loaded.Succeeded)
        {
            return new ServiceResult<ExpenseResponse>(loaded.Error);
        }

        Expense expense = loaded.Value;
        if (!expense.CanApply(ExpenseAction.Paid))
        {
            return new ServiceResult<ExpenseResponse>(
                ServiceError.Conflict($"Só reembolsos Approved podem ser pagos. Estado atual: {expense.Status}."));
        }

        expense.Pay(user.Id, _timeProvider.GetUtcNow().UtcDateTime);

        // status, PaymentRecord e histórico vão no mesmo SaveChanges: ou entram os três ou nenhum
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            return new ServiceResult<ExpenseResponse>(
                ServiceError.Conflict("O reembolso foi alterado por outra requisição. Consulte e tente de novo."));
        }

        return new ServiceResult<ExpenseResponse>(expense.ToResponse());
    }
}
