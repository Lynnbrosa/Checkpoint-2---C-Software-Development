using System;
using System.Collections.Generic;
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
/// Histórico de ações de um reembolso.
/// </summary>
[ApiController]
[Route("api/expenses/{id:guid}/history")]
[Authorize(Roles = Roles.ExpenseReaders)]
public sealed class ExpenseHistoryController : ControllerBase
{
    private readonly IExpenseHistoryService _history;

    /// <summary>Cria o controller.</summary>
    /// <param name="history">Serviço de histórico.</param>
    public ExpenseHistoryController(IExpenseHistoryService history)
    {
        _history = history;
    }

    /// <summary>Lista o histórico em ordem cronológica.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Entradas do histórico.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExpenseHistoryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ExpenseHistoryResponse>> result =
            await _history.GetHistoryAsync(id, UserContext.FromPrincipal(User), cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
