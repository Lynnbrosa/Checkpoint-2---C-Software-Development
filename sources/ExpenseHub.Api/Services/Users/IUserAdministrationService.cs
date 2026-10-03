using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services.Users;

/// <summary>
/// Administração de usuários e roles, exclusiva do Admin.
/// </summary>
public interface IUserAdministrationService
{
    /// <summary>Lista todos os usuários com suas roles.</summary>
    /// <param name="actor">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Usuários ordenados por e-mail.</returns>
    Task<ServiceResult<IReadOnlyList<UserResponse>>> ListAsync(UserContext actor, CancellationToken cancellationToken);

    /// <summary>Substitui as roles de um usuário.</summary>
    /// <param name="userId">Usuário alvo.</param>
    /// <param name="request">Roles desejadas.</param>
    /// <param name="actor">Admin que faz a alteração.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Usuário com as roles novas ou erro de negócio.</returns>
    Task<ServiceResult<UserResponse>> UpdateRolesAsync(
        string userId,
        UpdateUserRolesRequest request,
        UserContext actor,
        CancellationToken cancellationToken);
}
