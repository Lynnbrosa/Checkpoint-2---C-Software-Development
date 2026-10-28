namespace ExpenseHub.Api.Domain;

/// <summary>
/// Estado de processamento de uma solicitação de relatório.
/// </summary>
public enum ReportStatus
{
    /// <summary>Solicitação registrada e aguardando processamento pelo worker.</summary>
    Pending,

    /// <summary>Cálculo em andamento pelo worker.</summary>
    Processing,

    /// <summary>Cálculo concluído e resultado persistido.</summary>
    Completed,

    /// <summary>Falha definitiva registrada durante o processamento.</summary>
    Failed,
}

