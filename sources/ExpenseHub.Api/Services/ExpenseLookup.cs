using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

internal static class ExpenseLookup
{
    // role, escopo e dono resolvidos antes de qualquer serviço mexer no reembolso; o estado fica com quem chama
    public static async Task<ServiceResult<Expense>> LoadForAsync(
        this IExpenseRepository repository,
        Guid id,
        UserContext user,
        ExpenseOperation operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        string requiredRole = ExpenseAccessPolicy.RequiredRole(operation);
        if (!user.IsInRole(requiredRole))
        {
            return new ServiceResult<Expense>(ServiceError.Forbidden($"A operação exige a role {requiredRole}."));
        }

        Expense? expense = await repository.FindAsync(id, ExpenseAccessPolicy.ActionableBy(user, operation), cancellationToken);
        if (expense is null)
        {
            return new ServiceResult<Expense>(ServiceError.NotFound("Reembolso não encontrado."));
        }

        if (!ExpenseAccessPolicy.OwnershipAllows(expense, user, operation))
        {
            return new ServiceResult<Expense>(ServiceError.Forbidden(OwnershipMessage(operation)));
        }

        return new ServiceResult<Expense>(expense);
    }

    private static string OwnershipMessage(ExpenseOperation operation) => operation switch
    {
        ExpenseOperation.Decide => "Ninguém aprova ou reprova o próprio reembolso.",
        ExpenseOperation.Pay => "Ninguém paga o próprio reembolso.",
        _ => "Só o proprietário altera ou envia o reembolso.",
    };
}
