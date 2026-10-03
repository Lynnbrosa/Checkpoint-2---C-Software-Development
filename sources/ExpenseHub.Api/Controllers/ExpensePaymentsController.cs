using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Pagamento simulado de reembolsos aprovados.
/// </summary>
[ApiController]
[Route("api/expenses/{id:guid}")]
[Authorize(Roles = Roles.Finance)]
public sealed class ExpensePaymentsController : ControllerBase
{
    private readonly IExpensePaymentService _payments;

    /// <summary>Cria o controller.</summary>
    /// <param name="payments">Serviço de pagamento.</param>
    public ExpensePaymentsController(IExpensePaymentService payments)
    {
        _payments = payments;
    }

    /// <summary>Registra o pagamento. Ator e horário vêm do servidor.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Paid</c> com o registro de pagamento.</returns>
    [HttpPost("pay")]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _payments.PayAsync(id, UserContext.FromPrincipal(User), cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
