using System;
using System.Collections.Generic;
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
/// Reembolsos: criação, edição, envio e consulta conforme o perfil.
/// </summary>
[ApiController]
[Route("api/expenses")]
[Authorize]
public sealed class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenses;

    /// <summary>Cria o controller.</summary>
    /// <param name="expenses">Serviço de reembolsos.</param>
    public ExpensesController(IExpenseService expenses)
    {
        _expenses = expenses;
    }

    private UserContext CurrentUser => UserContext.FromPrincipal(User);

    /// <summary>Cria um rascunho. O proprietário é o usuário do token.</summary>
    /// <param name="request">Descrição, valor, data e categoria.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso criado em <c>Draft</c>.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(ExpenseDraftRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _expenses.CreateAsync(request, CurrentUser, cancellationToken);
        return this.ToActionResult(result, expense => Created($"/api/expenses/{expense.Id}", expense));
    }

    /// <summary>Edita um rascunho próprio.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos valores.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso atualizado.</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, ExpenseDraftRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _expenses.UpdateAsync(id, request, CurrentUser, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Lista os reembolsos que o perfil do usuário pode ver.</summary>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolsos visíveis.</returns>
    [HttpGet]
    [Authorize(Roles = Roles.ExpenseReaders)]
    [ProducesResponseType<IReadOnlyList<ExpenseResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ExpenseResponse>> result = await _expenses.ListAsync(CurrentUser, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Consulta um reembolso visível.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso ou 404.</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.ExpenseReaders)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _expenses.GetAsync(id, CurrentUser, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Envia um rascunho próprio para aprovação.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Submitted</c>.</returns>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = Roles.Employee)]
    [ProducesResponseType<ExpenseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ExpenseResponse> result = await _expenses.SubmitAsync(id, CurrentUser, cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
