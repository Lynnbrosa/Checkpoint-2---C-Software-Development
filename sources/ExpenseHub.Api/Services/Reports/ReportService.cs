using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Contracts.Requests;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;
using ExpenseHub.Api.Security;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Api.Services.Reports;

internal sealed class ReportService : IReportService
{
    private readonly IReportRepository _repository;
    private readonly IReportMessagePublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository repository,
        IReportMessagePublisher publisher,
        TimeProvider timeProvider,
        ILogger<ReportService> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ServiceResult<ReportResponse>> RequestAsync(
        CreateReportRequest request,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(user);

        if (!Enum.IsDefined(request.Scope))
        {
            return new ServiceResult<ReportResponse>(
                ServiceError.Validation(nameof(CreateReportRequest.Scope), "Escopo inválido. Use Mine ou Global."));
        }

        if (!ReportAccessPolicy.CanRequestScope(user, request.Scope))
        {
            return new ServiceResult<ReportResponse>(
                ServiceError.Forbidden($"O perfil não tem permissão para solicitar relatório com escopo {request.Scope}."));
        }

        if (request.FromDate == default)
        {
            return new ServiceResult<ReportResponse>(
                ServiceError.Validation(nameof(CreateReportRequest.FromDate), "A data inicial é obrigatória."));
        }

        if (request.ToDate == default)
        {
            return new ServiceResult<ReportResponse>(
                ServiceError.Validation(nameof(CreateReportRequest.ToDate), "A data final é obrigatória."));
        }

        if (request.FromDate > request.ToDate)
        {
            return new ServiceResult<ReportResponse>(
                ServiceError.Validation(
                    nameof(CreateReportRequest.FromDate),
                    "A data inicial deve ser menor ou igual à data final."));
        }

        ReportJob job = ReportJob.CreatePending(
            user.Id,
            request.Scope,
            request.FromDate,
            request.ToDate,
            _timeProvider.GetUtcNow().UtcDateTime);

        _repository.Add(job);
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            return new ServiceResult<ReportResponse>(
                ServiceError.Conflict("Não foi possível registrar a solicitação de relatório."));
        }

        bool published = await _publisher.TryPublishAsync(job.Id, cancellationToken);
        if (!published)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "ReportPublishUnconfirmed reportId={ReportId} scope={Scope}: solicitação registrada, mas sem confirmação do broker.",
                    job.Id,
                    job.Scope);
            }

            return new ServiceResult<ReportResponse>(
                ServiceError.ServiceUnavailable(
                    "Solicitação registrada, mas a publicação na fila não pôde ser confirmada.",
                    job.Id));
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "ReportPublished reportId={ReportId} scope={Scope} fromDate={FromDate} toDate={ToDate}",
                job.Id,
                job.Scope,
                job.FromDate,
                job.ToDate);
        }

        return new ServiceResult<ReportResponse>(job.ToResponse());
    }

    public async Task<ServiceResult<IReadOnlyList<ReportResponse>>> ListAsync(
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        IReadOnlyList<ReportJob> jobs = await _repository.ListByOwnerAsync(user.Id, cancellationToken);
        return new ServiceResult<IReadOnlyList<ReportResponse>>(jobs.Select(job => job.ToResponse()).ToList());
    }

    public async Task<ServiceResult<ReportResponse>> GetAsync(
        Guid id,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ReportJob? job = await _repository.FindOwnedByIdAsync(id, user.Id, cancellationToken);
        if (job is null || !ReportAccessPolicy.IsOwner(job, user))
        {
            return new ServiceResult<ReportResponse>(ServiceError.NotFound("Solicitação de relatório não encontrada."));
        }

        return new ServiceResult<ReportResponse>(job.ToResponse());
    }

    public async Task<ServiceResult<ReportExcelFile>> DownloadExcelAsync(
        Guid id,
        UserContext user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        ReportJob? job = await _repository.FindOwnedByIdAsync(id, user.Id, cancellationToken);
        if (job is null || !ReportAccessPolicy.IsOwner(job, user))
        {
            return new ServiceResult<ReportExcelFile>(ServiceError.NotFound("Solicitação de relatório não encontrada."));
        }

        if (job.Status != ReportStatus.Completed)
        {
            return new ServiceResult<ReportExcelFile>(
                ServiceError.Conflict($"O relatório ainda não está concluído para download. Estado atual: {job.Status}."));
        }

        ReportSummary? summary = job.GetSummary();
        if (summary is null)
        {
            return new ServiceResult<ReportExcelFile>(
                ServiceError.Conflict("O resultado persistido do relatório não está disponível."));
        }

        return new ServiceResult<ReportExcelFile>(ReportExcelGenerator.Generate(job, summary));
    }
}
