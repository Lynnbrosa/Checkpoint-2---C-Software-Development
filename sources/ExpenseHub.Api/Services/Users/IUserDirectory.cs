using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Services.Users;

// casca fina em volta do UserManager, pra testar as regras sem banco
internal interface IUserDirectory
{
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);

    Task<DirectoryUser?> FindAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DirectoryUser>> ListAsync(CancellationToken cancellationToken);

    // adiciona e remove na mesma transação
    Task<IdentityResult> ChangeRolesAsync(
        string userId,
        IReadOnlyCollection<string> rolesToAdd,
        IReadOnlyCollection<string> rolesToRemove,
        CancellationToken cancellationToken);
}
