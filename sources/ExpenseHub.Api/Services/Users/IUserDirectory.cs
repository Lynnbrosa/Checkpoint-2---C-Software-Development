using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Services.Users;

// casca fina em volta do UserManager, pra testar as regras sem banco
internal interface IUserDirectory
{
    Task<IdentityResult> CreateAsync(ApplicationUser user, string password);
}
