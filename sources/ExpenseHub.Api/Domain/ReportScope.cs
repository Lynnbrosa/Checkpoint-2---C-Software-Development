namespace ExpenseHub.Api.Domain;

/// <summary>
/// Escopo dos reembolsos incluídos em um relatório por período.
/// </summary>
public enum ReportScope
{
    /// <summary>Somente reembolsos do próprio solicitante.</summary>
    Mine,

    /// <summary>Reembolsos de todos os funcionários (requer perfil Auditor).</summary>
    Global,
}

