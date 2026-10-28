using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Identity;
using ExpenseHub.Api.Security;
using ExpenseHub.Api.Services;
using ExpenseHub.Api.Services.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseHub.Api.Controllers;

/// <summary>
/// Relatórios assíncronos de reembolsos por período: solicitação, acompanhamento e exportação Excel.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    /// <summary>Cria o controller de relatórios.</summary>
    /// <param name="reports">Serviço de relatórios.</param>
    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    private UserContext CurrentUser => UserContext.FromPrincipal(User);

    /// <summary>Solicita o cálculo assíncrono de um relatório de reembolsos por período.</summary>
    /// <param name="request">Período e escopo (<c>Mine</c> ou <c>Global</c>).</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Envelope inicial em <c>Pending</c> com cabeçalho <c>Location</c>.</returns>
    [HttpPost]
    [Authorize(Roles = Roles.ReportRequesters)]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RequestReport(CreateReportRequest request, CancellationToken cancellationToken)
    {
        ServiceResult<ReportResponse> result = await _reports.RequestAsync(request, CurrentUser, cancellationToken);
        return this.ToActionResult(result, report => Accepted($"/api/reports/{report.Id}", report));
    }

    /// <summary>Lista somente as solicitações criadas pelo próprio usuário autenticado.</summary>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Envelopes de solicitações do usuário.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ReportResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ServiceResult<IReadOnlyList<ReportResponse>> result = await _reports.ListAsync(CurrentUser, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Consulta o andamento e o resultado de uma solicitação própria.</summary>
    /// <param name="id">Identificador da solicitação.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Envelope atualizado ou 404.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ReportResponse> result = await _reports.GetAsync(id, CurrentUser, cancellationToken);
        return this.ToActionResult(result, Ok);
    }

    /// <summary>Baixa o resultado persistido de uma solicitação própria concluída em formato Excel (<c>.xlsx</c>).</summary>
    /// <param name="id">Identificador da solicitação.</param>
    /// <param name="cancellationToken">Cancelamento da requisição.</param>
    /// <returns>Planilha <c>.xlsx</c>, 404 se inexistente/alheia ou 409 se ainda não concluída.</returns>
    [HttpGet("{id:guid}/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DownloadExcel(Guid id, CancellationToken cancellationToken)
    {
        ServiceResult<ReportExcelFile> result = await _reports.DownloadExcelAsync(id, CurrentUser, cancellationToken);
        return this.ToActionResult(result, file => File(file.Content, file.ContentType, file.FileName));
    }
}

