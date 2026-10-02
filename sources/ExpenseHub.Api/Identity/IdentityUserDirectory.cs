using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Data;
using ExpenseHub.Api.Services.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ExpenseHub.Api.Identity;

internal sealed class IdentityUserDirectory : IUserDirectory
{
    private readonly ExpenseHubDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserDirectory(ExpenseHubDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Task<IdentityResult> CreateAsync(ApplicationUser user, string password) => _userManager.CreateAsync(user, password);

    public async Task<DirectoryUser?> FindAsync(string userId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return null;
        }

        IList<string> roles = await _userManager.GetRolesAsync(user);
        return new DirectoryUser(user.Id, user.Email ?? string.Empty, user.FullName, roles.ToList());
    }

    public async Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken)
    {
        // uma consulta só com as roles de cada um, em vez de um GetRolesAsync por usuário
        var rows = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Email)
            .Select(user => new
            {
                user.Id,
                user.Email,
                user.FullName,
                Roles = _context.UserRoles
                    .Where(link => link.UserId == user.Id)
                    .Join(_context.Roles, link => link.RoleId, role => role.Id, (link, role) => role.Name)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new DirectoryUser(
                row.Id,
                row.Email ?? string.Empty,
                row.FullName,
                row.Roles.OfType<string>().ToList()))
            .ToList();
    }

    public async Task<IdentityResult> ChangeRolesAsync(
        string userId,
        IReadOnlyCollection<string> rolesToAdd,
        IReadOnlyCollection<string> rolesToRemove,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"Usuário {userId} sumiu durante a troca de roles.");

        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (rolesToRemove.Count > 0)
        {
            IdentityResult removed = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removed.Succeeded)
            {
                return removed;
            }
        }

        if (rolesToAdd.Count > 0)
        {
            IdentityResult added = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!added.Succeeded)
            {
                return added;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return IdentityResult.Success;
    }
}
