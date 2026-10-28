using System;
using System.ComponentModel.DataAnnotations;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts.Requests;

/// <summary>
/// Parâmetros para solicitar um relatório de reembolsos por período.
/// </summary>
public sealed class CreateReportRequest
{
    /// <summary>Data inicial inclusiva (<c>yyyy-MM-dd</c>).</summary>
    [Required]
    public required DateOnly FromDate { get; init; }

    /// <summary>Data final inclusiva (<c>yyyy-MM-dd</c>).</summary>
    [Required]
    public required DateOnly ToDate { get; init; }

    /// <summary>Escopo do relatório (<c>Mine</c> ou <c>Global</c>).</summary>
    [Required]
    [EnumDataType(typeof(ReportScope))]
    public required ReportScope Scope { get; init; }
}

