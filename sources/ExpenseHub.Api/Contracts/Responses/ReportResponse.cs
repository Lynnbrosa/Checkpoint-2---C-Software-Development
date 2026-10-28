using System;
using System.Collections.Generic;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Contracts.Responses;

/// <summary>
/// Quantidade e soma de valores em um estado do reembolso.
/// </summary>
public sealed class ReportStatusSummaryResponse
{
    /// <summary>Estado do reembolso.</summary>
    public required ExpenseStatus Status { get; init; }

    /// <summary>Quantidade de reembolsos no estado.</summary>
    public required int Count { get; init; }

    /// <summary>Soma dos valores no estado.</summary>
    public required decimal Amount { get; init; }
}

/// <summary>
/// Resultado persistido de um relatório concluído.
/// </summary>
public sealed class ReportResultResponse
{
    /// <summary>Instante em que o resultado foi gerado pelo worker, em UTC.</summary>
    public required DateTime GeneratedAt { get; init; }

    /// <summary>Total de reembolsos considerados no período e escopo.</summary>
    public required int TotalCount { get; init; }

    /// <summary>Soma total dos valores considerados no período e escopo.</summary>
    public required decimal TotalAmount { get; init; }

    /// <summary>Resumo discriminado pelos cinco estados do reembolso.</summary>
    public required IReadOnlyList<ReportStatusSummaryResponse> ByStatus { get; init; }
}

/// <summary>
/// Erro sanitizado de uma solicitação em estado <c>Failed</c> ou com falha operacional.
/// </summary>
public sealed class ReportErrorResponse
{
    /// <summary>Código estável do erro.</summary>
    public required string Code { get; init; }

    /// <summary>Mensagem legível e sanitizada.</summary>
    public required string Message { get; init; }
}

/// <summary>
/// Envelope de uma solicitação de relatório e seu andamento/resultado.
/// </summary>
public sealed class ReportResponse
{
    /// <summary>Identificador gerado pelo servidor.</summary>
    public required Guid Id { get; init; }

    /// <summary>Escopo solicitado (<c>Mine</c> ou <c>Global</c>).</summary>
    public required ReportScope Scope { get; init; }

    /// <summary>Data inicial inclusiva.</summary>
    public required DateOnly FromDate { get; init; }

    /// <summary>Data final inclusiva.</summary>
    public required DateOnly ToDate { get; init; }

    /// <summary>Estado atual do processamento.</summary>
    public required ReportStatus Status { get; init; }

    /// <summary>Instante de criação em UTC.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Instante de início do cálculo pelo worker em UTC.</summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>Instante de conclusão (sucesso ou falha) em UTC.</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Resultado persistido quando o estado é <c>Completed</c>.</summary>
    public ReportResultResponse? Result { get; init; }

    /// <summary>Erro sanitizado quando o estado é <c>Failed</c>.</summary>
    public ReportErrorResponse? Error { get; init; }
}

/// <summary>
/// Arquivo Excel gerado a partir do resultado persistido de um relatório.
/// </summary>
public sealed class ReportExcelFile
{
    /// <summary>Nome sugerido para download (<c>.xlsx</c>).</summary>
    public required string FileName { get; init; }

    /// <summary>Tipo MIME oficial de planilha OpenXML.</summary>
    public required string ContentType { get; init; }

    /// <summary>Conteúdo binário da planilha <c>.xlsx</c>.</summary>
    public required byte[] Content { get; init; }
}

