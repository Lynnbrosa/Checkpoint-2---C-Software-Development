using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services;

/// <summary>
/// Criação, edição, envio e consulta de reembolsos.
/// </summary>
public interface IExpenseService
{
    /// <summary>Cria um rascunho com o usuário autenticado como proprietário.</summary>
    /// <param name="request">Campos do rascunho.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso criado ou erro de negócio.</returns>
    Task<ServiceResult<ExpenseResponse>> CreateAsync(
        ExpenseDraftRequest request,
        UserContext user,
        CancellationToken cancellationToken);

    /// <summary>Edita um rascunho próprio.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="request">Novos valores dos campos.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso atualizado ou erro de negócio.</returns>
    Task<ServiceResult<ExpenseResponse>> UpdateAsync(
        Guid id,
        ExpenseDraftRequest request,
        UserContext user,
        CancellationToken cancellationToken);

    /// <summary>Envia um rascunho próprio para aprovação.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso em <c>Submitted</c> ou erro de negócio.</returns>
    Task<ServiceResult<ExpenseResponse>> SubmitAsync(Guid id, UserContext user, CancellationToken cancellationToken);

    /// <summary>Lista os reembolsos visíveis para o perfil do usuário.</summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolsos visíveis, mais recentes primeiro.</returns>
    Task<ServiceResult<IReadOnlyList<ExpenseResponse>>> ListAsync(UserContext user, CancellationToken cancellationToken);

    /// <summary>Consulta um reembolso visível para o usuário.</summary>
    /// <param name="id">Identificador do reembolso.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Reembolso ou 404 quando inexistente ou fora do escopo.</returns>
    Task<ServiceResult<ExpenseResponse>> GetAsync(Guid id, UserContext user, CancellationToken cancellationToken);
}
