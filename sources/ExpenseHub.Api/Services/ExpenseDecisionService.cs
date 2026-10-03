using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal sealed class ExpenseDecisionService : IExpenseDecisionService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpenseDecisionService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<ExpenseResponse>> ApproveAsync(
        Guid id,
        UserContext user,
        CancellationToken cancellationToken)
    {
        // role Approver, escopo e "não é o dono" ficam no LoadForAsync; aqui sobra o estado
        ServiceResult<Expense> loaded = await _repository.LoadForAsync(id, user, ExpenseOperation.Decide, cancellationToken);
        if (!loaded.Succeeded)
        {
            return Fail(loaded.Error);
        }

        Expense expense = loaded.Value;
        if (!expense.CanApply(ExpenseAction.Approved))
        {
            return Fail(NotAwaitingDecision(expense));
        }

        expense.Approve(user.Id, _timeProvider.GetUtcNow().UtcDateTime);
        return await SaveAsync(expense, cancellationToken);
    }

    public async Task<ServiceResult<ExpenseResponse>> RejectAsync(
        Guid id,
        RejectExpenseRequest request,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ServiceResult<Expense> loaded = await _repository.LoadForAsync(id, user, ExpenseOperation.Decide, cancellationToken);
        if (!loaded.Succeeded)
        {
            return Fail(loaded.Error);
        }

        if (!ExpenseRules.IsValidJustification(request.Justification))
        {
            return Fail(ServiceError.Validation(
                nameof(RejectExpenseRequest.Justification),
                "A justificativa deve ter entre 10 e 500 caracteres."));
        }

        Expense expense = loaded.Value;
        if (!expense.CanApply(ExpenseAction.Rejected))
        {
            return Fail(NotAwaitingDecision(expense));
        }

        expense.Reject(user.Id, request.Justification, _timeProvider.GetUtcNow().UtcDateTime);
        return await SaveAsync(expense, cancellationToken);
    }

    private async Task<ServiceResult<ExpenseResponse>> SaveAsync(Expense expense, CancellationToken cancellationToken)
    {
        // dois approvers ao mesmo tempo: o segundo cai aqui e não grava histórico duplicado
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            return Fail(ServiceError.Conflict("O reembolso foi decidido por outra requisição. Consulte e tente de novo."));
        }

        return new ServiceResult<ExpenseResponse>(expense.ToResponse());
    }

    private static ServiceError NotAwaitingDecision(Expense expense) =>
        ServiceError.Conflict($"Só reembolsos Submitted recebem decisão. Estado atual: {expense.Status}.");

    private static ServiceResult<ExpenseResponse> Fail(ServiceError error) => new(error);
}
