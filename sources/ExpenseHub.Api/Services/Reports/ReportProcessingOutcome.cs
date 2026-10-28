namespace ExpenseHub.Api.Services.Reports;

/// <summary>
/// Desfecho do processamento de uma mensagem de relatório pelo worker.
/// </summary>
public enum ReportProcessingOutcome
{
    /// <summary>Relatório calculado e persistido com sucesso (confirmar consumo).</summary>
    Completed,

    /// <summary>Solicitação já estava em estado final; duplicata ignorada sem novo efeito (confirmar consumo).</summary>
    DuplicateIgnored,

    /// <summary>Falha definitiva registrada no banco (confirmar consumo sem reencaminhar).</summary>
    FailedPermanently,

    /// <summary>Mensagem referente a um identificador inexistente (confirmar para descartar sem loop infinito).</summary>
    NotFound,

    /// <summary>Falha transitória ao persistir; não confirmar consumo para permitir reentrega.</summary>
    TransientFailure,
}

