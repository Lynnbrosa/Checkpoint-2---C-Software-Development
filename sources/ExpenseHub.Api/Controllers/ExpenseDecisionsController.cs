using System;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Aprovação e reprovação de reembolsos enviados.
/// </summary>
[ApiController]
[Route("api/expenses/{id:guid}")]
[Authorize(Roles = Roles.Approver)]
public sealed class ExpenseDecisionsController : ControllerBase
{
    private readonly IExpenseDecisionService _decisions;

    /// <summary>Cria o controller.</summary>
    /// <param name="decisions">Serviço de decisão.</param>
    public ExpenseDecisionsController(IExpenseDecisionService decisions)
    {
        _decisions = decisions;
    }

    /// <summary>Aprova um reembolso <c>Submitted</c>. Ator e horário vêm do servidor.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Approved</c>.</returns>
    [HttpPost("approve")]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result =
            await _decisions.ApproveAsync(id, UserContext.FromPrincipal(User), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Reprova um reembolso <c>Submitted</c>. A justificativa é obrigatória.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Justificativa, entre 10 e 500 caracteres.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Rejected</c>.</returns>
    [HttpPost("reject")]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(Guid id, RejectExpenseRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result =
            await _decisions.RejectAsync(id, request, UserContext.FromPrincipal(User), cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
