using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Identity;

/// <summary>
/// Usuário do ExpenseHub persistido pelo ASP.NET Core Identity.
/// </summary>
public sealed class ApplicationUser : IdentityUser
{
    /// <summary>Nome exibido nas listagens.</summary>
    public string FullName { get; set; } = string.Empty;
}
