using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Decisão do Approver sobre reembolsos enviados.
/// </summary>
public interface IExpenseDecisionService
{
    /// <summary>Aprova um reembolso <c>Submitted</c> de outra pessoa.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="user">Approver autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Approved</c> ou erro de negócio.</returns>
    Task<ServiceResult<ExpenseResponse>> ApproveAsync(Guid id, UserContext user, CancellationToken cancellationToken);
}
