using System;

namespace ExpenseHub.Api.Messaging;

/// <summary>
/// Contrato de mensagem publicada no RabbitMQ para solicitar o cálculo de um relatório.
/// </summary>
public sealed class ReportRequestedMessage
{
    /// <summary>Identificador persistido da solicitação de relatório.</summary>
    public required Guid ReportId { get; init; }
}

