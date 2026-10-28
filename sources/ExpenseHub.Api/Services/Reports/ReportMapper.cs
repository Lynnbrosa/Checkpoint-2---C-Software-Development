using System.Linq;
using ExpenseHub.Api.Contracts.Responses;
using ExpenseHub.Api.Domain;

namespace ExpenseHub.Api.Services.Reports;

internal static class ReportMapper
{
    public static ReportResponse ToResponse(this ReportJob job)
    {
        ReportSummary? summary = job.GetSummary();

        return new ReportResponse
        {
            Id = job.Id,
            Scope = job.Scope,
            FromDate = job.FromDate,
            ToDate = job.ToDate,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            Result = summary is null
                ? null
                : new ReportResultResponse
                {
                    GeneratedAt = summary.GeneratedAt,
                    TotalCount = summary.TotalCount,
                    TotalAmount = summary.TotalAmount,
                    ByStatus = summary.ByStatus
                        .Select(item => new ReportStatusSummaryResponse
                        {
                            Status = item.Status,
                            Count = item.Count,
                            Amount = item.Amount,
                        })
                        .ToList(),
                },
            Error = job.Status == ReportStatus.Failed && job.ErrorCode is not null && job.ErrorMessage is not null
                ? new ReportErrorResponse
                {
                    Code = job.ErrorCode,
                    Message = job.ErrorMessage,
                }
                : null,
        };
    }
}

