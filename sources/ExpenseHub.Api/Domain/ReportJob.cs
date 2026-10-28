using System;

namespace ExpenseHub.Api.Domain;

internal sealed class ReportJob
{
    private ReportJob()
    {
    }

    public Guid Id { get; private set; }

    public string OwnerId { get; private set; } = string.Empty;

    public ReportScope Scope { get; private set; }

    public DateOnly FromDate { get; private set; }

    public DateOnly ToDate { get; private set; }

    public ReportStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    public string? ResultJson { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? ErrorMessage { get; private set; }

    public Guid ConcurrencyStamp { get; private set; }

    public bool IsTerminal => Status is ReportStatus.Completed or ReportStatus.Failed;

    public static ReportJob CreatePending(
        string ownerId,
        ReportScope scope,
        DateOnly fromDate,
        DateOnly toDate,
        DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        if (fromDate > toDate)
        {
            throw new ArgumentException("A data inicial deve ser menor ou igual à data final.", nameof(fromDate));
        }

        return new ReportJob
        {
            Id = Guid.CreateVersion7(),
            OwnerId = ownerId,
            Scope = scope,
            FromDate = fromDate,
            ToDate = toDate,
            Status = ReportStatus.Pending,
            CreatedAt = nowUtc,
            ConcurrencyStamp = Guid.NewGuid(),
        };
    }

    public void MarkProcessing(DateTime nowUtc)
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException($"A solicitação já está no estado final {Status}.");
        }

        Status = ReportStatus.Processing;
        StartedAt ??= nowUtc;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void Complete(ReportSummary summary, DateTime completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(summary);
        if (IsTerminal)
        {
            throw new InvalidOperationException($"A solicitação já está no estado final {Status}.");
        }

        StartedAt ??= completedAtUtc;
        CompletedAt = completedAtUtc;
        Status = ReportStatus.Completed;
        ResultJson = ReportResultSerializer.Serialize(summary);
        ErrorCode = null;
        ErrorMessage = null;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public void Fail(string errorCode, string errorMessage, DateTime completedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        if (IsTerminal)
        {
            throw new InvalidOperationException($"A solicitação já está no estado final {Status}.");
        }

        StartedAt ??= completedAtUtc;
        CompletedAt = completedAtUtc;
        Status = ReportStatus.Failed;
        ResultJson = null;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public ReportSummary? GetSummary() =>
        Status == ReportStatus.Completed ? ReportResultSerializer.Deserialize(ResultJson) : null;
}

