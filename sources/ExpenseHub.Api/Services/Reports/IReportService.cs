using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Security;

namespace ExpenseHub.Api.Services.Reports;

/// <summary>
/// Solicitação, acompanhamento e exportação de relatórios de reembolsos por período.
/// </summary>
public interface IReportService
{
    /// <summary>Registra uma nova solicitação de relatório e publica na fila.</summary>
    /// <param name="request">Período e escopo desejados.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Envelope em <c>Pending</c> quando publicado ou erro de negócio/indisponibilidade.</returns>
    Task<ServiceResult<ReportResponse>> RequestAsync(
        CreateReportRequest request,
        UserContext user,
        CancellationToken cancellationToken);

    /// <summary>Lista somente as solicitações criadas pelo próprio usuário autenticado.</summary>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Envelopes do solicitante.</returns>
    Task<ServiceResult<IReadOnlyList<ReportResponse>>> ListAsync(
        UserContext user,
        CancellationToken cancellationToken);

    /// <summary>Consulta o andamento e o resultado de uma solicitação própria.</summary>
    /// <param name="id">Identificador da solicitação.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Envelope atualizado ou 404.</returns>
    Task<ServiceResult<ReportResponse>> GetAsync(
        Guid id,
        UserContext user,
        CancellationToken cancellationToken);

    /// <summary>Gera a planilha Excel a partir do resultado persistido de uma solicitação própria concluída.</summary>
    /// <param name="id">Identificador da solicitação.</param>
    /// <param name="user">Usuário autenticado.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Arquivo <c>.xlsx</c>, 404 para inexistente/alheio ou 409 se não concluído.</returns>
    Task<ServiceResult<ReportExcelFile>> DownloadExcelAsync(
        Guid id,
        UserContext user,
        CancellationToken cancellationToken);
}

