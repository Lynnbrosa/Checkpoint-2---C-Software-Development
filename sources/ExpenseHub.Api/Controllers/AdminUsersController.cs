using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Usuários e roles, só para Admin.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IUserAdministrationService _users;

    /// <summary>Cria o controller.</summary>
    /// <param name="users">Serviço de administração de usuários.</param>
    public AdminUsersController(IUserAdministrationService users)
    {
        _users = users;
    }

    /// <summary>Lista os usuários e as roles de cada um.</summary>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Usuários ordenados por e-mail.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<UserResponse>> result =
            await _users.ListAsync(UserContext.FromPrincipal(User), cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Substitui as roles do usuário. O usuário precisa fazer login de novo depois.</summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="request">Lista completa de roles desejadas.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Usuário com as roles novas.</returns>
    [HttpPut("{id}/roles")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRoles(
        string id,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<UserResponse> result =
            await _users.UpdateRolesAsync(id, request, UserContext.FromPrincipal(User), cancellationToken);
        return this.ToActionResult(result, Ok);
    }
}
