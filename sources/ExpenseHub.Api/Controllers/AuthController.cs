using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using IdentitySignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Autenticação por token bearer.
/// </summary>
[ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>Cria o controller com os serviços do Identity.</summary>
    /// <param name="signInManager">Gerenciador de login do Identity.</param>
    /// <param name="userManager">Gerenciador de usuários do Identity.</param>
    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>Autentica o usuário e devolve o token bearer.</summary>
    /// <param name="request">E-mail e senha.</param>
    /// <returns>Token de acesso ou 401.</returns>
    [HttpPost("/login")]
    [AllowAnonymous]
    [ProducesResponseType<AccessTokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return InvalidCredentials();
        }

        _signInManager.AuthenticationScheme = IdentityConstants.BearerScheme;
        IdentitySignInResult result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Problem(
                title: "Conta bloqueada temporariamente.",
                detail: "Muitas tentativas inválidas. Tente de novo em alguns minutos.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.Succeeded)
        {
            return InvalidCredentials();
        }

        // o handler de bearer já escreveu o token no corpo durante o sign-in
        return new EmptyResult();
    }

    /// <summary>Mostra quem está autenticado e as roles do token atual.</summary>
    /// <returns>Identificador, e-mail e roles.</returns>
    [HttpGet("/me")]
    [Authorize]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Me()
    {
        UserContext current = UserContext.FromPrincipal(User);

        return new CurrentUserResponse
        {
            Id = current.Id,
            Email = User.FindFirstValue(ClaimTypes.Email),
            Roles = current.Roles.Order().ToList(),
        };
    }

    // mesma resposta pra e-mail inexistente e senha errada, pra não revelar quem tem conta
    private ObjectResult InvalidCredentials() => Problem(
        title: "Credenciais inválidas.",
        statusCode: StatusCodes.Status401Unauthorized);
}
