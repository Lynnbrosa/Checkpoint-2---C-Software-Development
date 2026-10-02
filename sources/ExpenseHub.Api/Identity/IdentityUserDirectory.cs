using System.Threading.Tasks;
using ExpenseHub.Api.Services.Users;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Identity;

internal sealed class IdentityUserDirectory : IUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserDirectory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<IdentityResult> CreateAsync(ApplicationUser user, string password) => _userManager.CreateAsync(user, password);
}
