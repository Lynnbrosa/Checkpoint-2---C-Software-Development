using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Consulta do histórico de um reembolso.
/// </summary>
public interface IExpenseHistoryService
{
    /// <summary>Histórico em ordem cronológica, com a mesma visibilidade do reembolso.</summary>
    /// <param name="expenseId">Identificador do reembolso.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Entradas do histórico ou 404 se o reembolso não é visível.</returns>
    Task<ServiceResult<IReadOnlyList<ExpenseHistoryResponse>>> GetHistoryAsync(
        Guid expenseId,
        UserContext user,
        CancellationToken cancellationToken);
}
