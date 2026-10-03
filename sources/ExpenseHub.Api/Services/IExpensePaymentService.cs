using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Registro de pagamento simulado pelo Finance.
/// </summary>
public interface IExpensePaymentService
{
    /// <summary>Registra o pagamento de um reembolso <c>Approved</c> de outra pessoa.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="user">Usuário do Finance autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Paid</c> com o registro de pagamento, ou erro de negócio.</returns>
    Task<ServiceResult<ExpenseResponse>> PayAsync(Guid id, UserContext user, CancellationToken cancellationToken);
}
