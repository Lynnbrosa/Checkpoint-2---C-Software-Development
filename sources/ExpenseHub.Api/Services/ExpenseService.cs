using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal sealed class ExpenseService : IExpenseService
{
    private readonly IExpenseRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExpenseService(IExpenseRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<ServiceResult<ExpenseResponse>> CreateAsync(
        ExpenseDraftRequest request,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(user);

        // o atributo no controller já barra, mas a regra fica aqui também
        if (!user.IsInRole(Roles.Employee))
        {
            return Fail(ServiceError.Forbidden("Somente Employee cria reembolsos."));
        }

        ServiceError? invalid = Validate(request);
        if (invalid is not null)
        {
            return Fail(invalid);
        }

        ExpenseCategory? category = await _repository.FindCategoryAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Fail(UnknownCategory());
        }

        Expense expense = Expense.CreateDraft(
            user.Id,
            category,
            request.Description,
            request.Amount,
            request.ExpenseDate,
            UtcNow());

        _repository.Add(expense);
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            return Fail(ConcurrentChange());
        }

        return new ServiceResult<ExpenseResponse>(expense.ToResponse());
    }

    public async Task<ServiceResult<ExpenseResponse>> UpdateAsync(
        Guid id,
        ExpenseDraftRequest request,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(user);

        if (!user.IsInRole(Roles.Employee))
        {
            return Fail(ServiceError.Forbidden("Somente Employee edita reembolsos."));
        }

        ServiceError? invalid = Validate(request);
        if (invalid is not null)
        {
            return Fail(invalid);
        }

        // rascunho de outra pessoa não existe pra quem pergunta: 404, não 403
        string userId = user.Id;
        Expense? expense = await _repository.FindAsync(id, candidate => candidate.OwnerId == userId, cancellationToken);
        if (expense is null)
        {
            return Fail(NotFound());
        }

        if (!expense.CanApply(ExpenseAction.Updated))
        {
            return Fail(ServiceError.Conflict($"Só rascunhos podem ser editados. Estado atual: {expense.Status}."));
        }

        ExpenseCategory? category = await _repository.FindCategoryAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Fail(UnknownCategory());
        }

        expense.UpdateDraft(category, request.Description, request.Amount, request.ExpenseDate, user.Id, UtcNow());
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            return Fail(ConcurrentChange());
        }

        return new ServiceResult<ExpenseResponse>(expense.ToResponse());
    }

    private ServiceError? Validate(ExpenseDraftRequest request)
    {
        if (!ExpenseRules.IsValidDescription(request.Description))
        {
            return ServiceError.Validation(
                nameof(ExpenseDraftRequest.Description),
                "A descrição deve ter entre 10 e 500 caracteres.");
        }

        if (!ExpenseRules.IsValidAmount(request.Amount))
        {
            return ServiceError.Validation(
                nameof(ExpenseDraftRequest.Amount),
                "O valor deve estar entre R$ 0,01 e R$ 2.147.483.647,00.");
        }

        if (!ExpenseRules.IsValidExpenseDate(request.ExpenseDate, Today()))
        {
            return ServiceError.Validation(
                nameof(ExpenseDraftRequest.ExpenseDate),
                "A data da despesa não pode ser futura.");
        }

        return null;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    // "futura" é em relação ao dia do servidor, não ao UTC, senão às 21h de SP já seria amanhã
    private DateOnly Today() => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    private static ServiceError UnknownCategory() =>
        ServiceError.Validation(nameof(ExpenseDraftRequest.CategoryId), "Categoria inexistente.");

    private static ServiceError NotFound() => ServiceError.NotFound("Reembolso não encontrado.");

    private static ServiceError ConcurrentChange() =>
        ServiceError.Conflict("O reembolso foi alterado por outra requisição. Consulte e tente de novo.");

    private static ServiceResult<ExpenseResponse> Fail(ServiceError error) => new(error);
}
