using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpenseHub.Api.Services.Reports;

/// <summary>
/// Processador de solicitações de relatório invocado pelo worker após receber uma mensagem da fila.
/// </summary>
public interface IReportProcessor
{
    /// <summary>Processa a solicitação identificada por <paramref name="reportId"/> de forma idempotente.</summary>
    /// <param name="reportId">Identificador do relatório.</param>
    /// <param name="cancellationToken">Cancelamento da operação.</param>
    /// <returns>Desfecho indicando se o consumo da mensagem pode ser confirmado.</returns>
    Task<ReportProcessingOutcome> ProcessAsync(Guid reportId, CancellationToken cancellationToken);
}

