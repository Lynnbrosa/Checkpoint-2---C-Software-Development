using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Domain;
using Microsoft.Extensions.Logging;

namespace ExpenseHub.Api.Services.Reports;

internal sealed class ReportProcessor : IReportProcessor
{
    private readonly IReportRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReportProcessor> _logger;

    public ReportProcessor(
        IReportRepository repository,
        TimeProvider timeProvider,
        ILogger<ReportProcessor> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ReportProcessingOutcome> ProcessAsync(Guid reportId, CancellationToken cancellationToken)
    {
        ReportJob? job = await _repository.FindByIdAsync(reportId, cancellationToken);
        if (job is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("ReportNotFound reportId={ReportId}: registro não encontrado no banco.", reportId);
            }

            return ReportProcessingOutcome.NotFound;
        }

        if (job.IsTerminal)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "ReportDuplicateIgnored reportId={ReportId} status={Status}: solicitação já concluída, nenhum novo efeito gerado.",
                    job.Id,
                    job.Status);
            }

            return ReportProcessingOutcome.DuplicateIgnored;
        }

        DateTime startedAt = _timeProvider.GetUtcNow().UtcDateTime;
        job.MarkProcessing(startedAt);
        if (!await _repository.TrySaveChangesAsync(cancellationToken))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "ReportStateConflict reportId={ReportId}: conflito ao registrar início do processamento.",
                    job.Id);
            }

            return ReportProcessingOutcome.TransientFailure;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "ReportStarted reportId={ReportId} scope={Scope} fromDate={FromDate} toDate={ToDate}",
                job.Id,
                job.Scope,
                job.FromDate,
                job.ToDate);
        }

        if (job.FromDate > job.ToDate || !Enum.IsDefined(job.Scope) || string.IsNullOrWhiteSpace(job.OwnerId))
        {
            return await RecordPermanentFailureAsync(
                job,
                "InvalidReportParameters",
                "Os parâmetros persistidos da solicitação são inválidos para cálculo.",
                cancellationToken);
        }

        try
        {
            IReadOnlyList<Expense> expenses = await _repository.ListEligibleExpensesAsync(
                job.FromDate,
                job.ToDate,
                job.Scope,
                job.OwnerId,
                cancellationToken);

            DateTime completedAt = _timeProvider.GetUtcNow().UtcDateTime;
            ReportSummary summary = ReportSummary.Calculate(expenses, completedAt);
            job.Complete(summary, completedAt);

            if (!await _repository.TrySaveChangesAsync(cancellationToken))
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(
                        "ReportPersistConflict reportId={ReportId}: falha ao persistir conclusão do relatório.",
                        job.Id);
                }

                return ReportProcessingOutcome.TransientFailure;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "ReportCompleted reportId={ReportId} totalCount={TotalCount} totalAmount={TotalAmount}",
                    job.Id,
                    summary.TotalCount,
                    summary.TotalAmount);
            }

            return ReportProcessingOutcome.Completed;
        }
        catch (ArgumentException ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "ReportPermanentError reportId={ReportId}: erro de negócio definitivo.", job.Id);
            }

            return await RecordPermanentFailureAsync(
                job,
                "CalculationError",
                "Não foi possível calcular o relatório devido a parâmetros inválidos.",
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "ReportTransientError reportId={ReportId}: erro transitório ao calcular ou persistir.",
                    job.Id);
            }

            return ReportProcessingOutcome.TransientFailure;
        }
    }

    private async Task<ReportProcessingOutcome> RecordPermanentFailureAsync(
        ReportJob job,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            DateTime completedAt = _timeProvider.GetUtcNow().UtcDateTime;
            job.Fail(errorCode, errorMessage, completedAt);
            if (!await _repository.TrySaveChangesAsync(cancellationToken))
            {
                return ReportProcessingOutcome.TransientFailure;
            }

            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "ReportFailed reportId={ReportId} code={ErrorCode}: falha definitiva persistida.",
                    job.Id,
                    errorCode);
            }

            return ReportProcessingOutcome.FailedPermanently;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    ex,
                    "ReportFailurePersistError reportId={ReportId}: não foi possível salvar o estado Failed.",
                    job.Id);
            }

            return ReportProcessingOutcome.TransientFailure;
        }
    }
}
