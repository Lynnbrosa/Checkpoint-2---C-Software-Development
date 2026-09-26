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
/// Rascunhos de reembolso do usuário autenticado.
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
}
