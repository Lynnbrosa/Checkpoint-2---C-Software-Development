using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpenseHub.Api.Services.Reports;

/// <summary>
/// Publicador de solicitações de relatório para processamento assíncrono.
/// </summary>
public interface IReportMessagePublisher
{
    /// <summary>
    /// Publica a mensagem de processamento do relatório e aguarda confirmação do broker.
    /// </summary>
    /// <param name="reportId">Identificador do relatório persistido.</param>
    /// <param name="cancellationToken">Cancelamento da operação.</param>
    /// <returns><see langword="true"/> quando o broker confirmou o recebimento da mensagem.</returns>
    Task<bool> TryPublishAsync(Guid reportId, CancellationToken cancellationToken);
}

