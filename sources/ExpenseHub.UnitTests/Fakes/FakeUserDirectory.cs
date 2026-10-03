using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Services.Users;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.UnitTests.Fakes;

internal sealed class FakeUserDirectory : IUserDirectory
{
    private readonly Dictionary<string, DirectoryUser> _users = new(StringComparer.Ordinal);

    public int RoleChanges { get; private set; }

    // o que o Identity devolveria no CreateAsync; null = sucesso
    public IdentityError[]? CreateErrors { get; set; }

    public ApplicationUser? LastCreated { get; private set; }

    public DirectoryUser Add(string id, string email, params string[] roles)
    {
        DirectoryUser user = new(id, email, email, roles);
        _users[id] = user;
        return user;
    }

    public IReadOnlyList<string> RolesOf(string id) => _users[id].Roles;

    public Task<IdentityResult> CreateAsync(ApplicationUser user, string password)
    {
        if (CreateErrors is not null)
        {
            return Task.FromResult(IdentityResult.Failed(CreateErrors));
        }

        LastCreated = user;
        _users[user.Id] = new DirectoryUser(user.Id, user.Email ?? string.Empty, user.FullName, []);
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<DirectoryUser?> FindAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult(_users.GetValueOrDefault(userId));

    public Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DirectoryUser>>(_users.Values.OrderBy(user => user.Email, StringComparer.Ordinal).ToList());

    public Task<IdentityResult> ChangeRolesAsync(
        string userId,
        IReadOnlyCollection<string> rolesToAdd,
        IReadOnlyCollection<string> rolesToRemove,
        CancellationToken cancellationToken)
    {
        DirectoryUser user = _users[userId];
        List<string> roles = user.Roles.Except(rolesToRemove).Concat(rolesToAdd).ToList();
        _users[userId] = user with { Roles = roles };
        RoleChanges++;
        return Task.FromResult(IdentityResult.Success);
    }
}
