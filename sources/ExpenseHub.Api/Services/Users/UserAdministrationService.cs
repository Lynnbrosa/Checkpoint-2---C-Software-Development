using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Services.Users;

internal sealed class UserAdministrationService : IUserAdministrationService
{
    private readonly IUserDirectory _directory;

    public UserAdministrationService(IUserDirectory directory)
    {
        _directory = directory;
    }

    public async Task<ServiceResult<IReadOnlyList<UserResponse>>> ListAsync(
        UserContext actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!actor.IsInRole(Roles.Admin))
        {
            return new ServiceResult<IReadOnlyList<UserResponse>>(ServiceError.Forbidden("Somente Admin lista usuários."));
        }

        IReadOnlyList<DirectoryUser> users = await _directory.ListAsync(cancellationToken);
        return new ServiceResult<IReadOnlyList<UserResponse>>(users.Select(user => user.ToResponse()).ToList());
    }

    public async Task<ServiceResult<UserResponse>> UpdateRolesAsync(
        string userId,
        UpdateUserRolesRequest request,
        UserContext actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(actor);

        if (!actor.IsInRole(Roles.Admin))
        {
            return Fail(ServiceError.Forbidden("Somente Admin altera roles."));
        }

        // role fora da lista fixa não é criada no caminho, nem com outra caixa ("admin")
        List<string> unknown = request.Roles
            .Where(role => !Roles.All.Contains(role, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (unknown.Count > 0)
        {
            return Fail(ServiceError.Validation(
                nameof(UpdateUserRolesRequest.Roles),
                $"Roles desconhecidas: {string.Join(", ", unknown.Select(role => $"'{role}'"))}. "
                + $"Válidas: {string.Join(", ", Roles.All)}."));
        }

        DirectoryUser? target = await _directory.FindAsync(userId, cancellationToken);
        if (target is null)
        {
            return Fail(ServiceError.NotFound("Usuário não encontrado."));
        }

        HashSet<string> desired = request.Roles.ToHashSet(StringComparer.Ordinal);

        // sem isso o Admin podia se trancar pra fora da administração
        if (string.Equals(target.Id, actor.Id, StringComparison.Ordinal) && !desired.Contains(Roles.Admin))
        {
            return Fail(ServiceError.Forbidden("O Admin não pode remover a própria role Admin."));
        }

        List<string> toAdd = desired.Except(target.Roles, StringComparer.Ordinal).ToList();
        List<string> toRemove = target.Roles.Except(desired, StringComparer.Ordinal).ToList();
        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            return new ServiceResult<UserResponse>(target.ToResponse());
        }

        IdentityResult changed = await _directory.ChangeRolesAsync(target.Id, toAdd, toRemove, cancellationToken);
        if (!changed.Succeeded)
        {
            throw new InvalidOperationException(
                "Falha ao alterar roles: " + string.Join("; ", changed.Errors.Select(error => error.Description)));
        }

        return new ServiceResult<UserResponse>((target with { Roles = desired.ToList() }).ToResponse());
    }

    private static ServiceResult<UserResponse> Fail(ServiceError error) => new(error);
}
