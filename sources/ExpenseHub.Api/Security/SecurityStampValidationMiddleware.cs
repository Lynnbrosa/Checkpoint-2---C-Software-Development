using System.Threading.Tasks;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Security;

// o token bearer guarda as roles do momento do login. trocar roles muda o security stamp,
// e aqui o token antigo deixa de valer; sem isso ele seguiria com as roles velhas até expirar
internal sealed class SecurityStampValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityStampValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, SignInManager<ApplicationUser> signInManager)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && await signInManager.ValidateSecurityStampAsync(context.User) is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\", error_description=\"login novamente\"";
            return;
        }

        await _next(context);
    }
}
