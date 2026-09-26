using ExpenseHub.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseHub.Api.Identity;

internal static class IdentityRegistration
{
    public static IServiceCollection AddExpenseHubIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        // token bearer do próprio Identity: sem chave de assinatura pra guardar em configuração
        services.AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);
        services.AddAuthorization();

        services.AddIdentityCore<ApplicationUser>(options => options.User.RequireUniqueEmail = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ExpenseHubDbContext>()
            .AddSignInManager();

        return services;
    }
}
