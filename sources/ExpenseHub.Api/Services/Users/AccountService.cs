using System;
using System.Linq;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using Microsoft.AspNetCore.Identity;

namespace ExpenseHub.Api.Services.Users;

internal sealed class AccountService : IAccountService
{
    private readonly IUserDirectory _directory;

    public AccountService(IUserDirectory directory)
    {
        _directory = directory;
    }

    public async Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // o request nem tem campo de role; o usuário nasce sem nenhuma e espera o Admin
        ApplicationUser user = new()
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName?.Trim() ?? string.Empty,
        };

        IdentityResult result = await _directory.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new ServiceResult<UserResponse>(ToError(result));
        }

        return new ServiceResult<UserResponse>(new DirectoryUser(user.Id, request.Email, user.FullName, []).ToResponse());
    }

    private static ServiceError ToError(IdentityResult result)
    {
        if (result.Errors.Any(error =>
            error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return ServiceError.Conflict("Já existe um usuário com esse e-mail.");
        }

        string field = result.Errors.All(error => error.Code.StartsWith("Password", StringComparison.Ordinal))
            ? nameof(RegisterRequest.Password)
            : nameof(RegisterRequest.Email);

        return ServiceError.Validation(field, string.Join(" ", result.Errors.Select(error => error.Description)));
    }
}
