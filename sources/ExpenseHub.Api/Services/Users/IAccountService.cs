using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;

namespace ExpenseHub.Api.Services.Users;

/// <summary>
/// Cadastro público de usuários.
/// </summary>
public interface IAccountService
{
    /// <summary>Cria um usuário sem nenhuma role.</summary>
    /// <param name="request">E-mail, senha e nome.</param>
    /// <returns>Usuário criado ou erro de validação ou conflito.</returns>
    Task<ServiceResult<UserResponse>> RegisterAsync(RegisterRequest request);
}
