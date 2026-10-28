using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExpenseHub.Api.Services.Reports;

namespace ExpenseHub.UnitTests.Fakes;

internal sealed class FakeReportMessagePublisher : IReportMessagePublisher
{
    private readonly List<Guid> _publishedIds = [];

    public IReadOnlyList<Guid> PublishedIds => _publishedIds;

    public bool ConfirmNextPublish { get; set; } = true;

    public Task<bool> TryPublishAsync(Guid reportId, CancellationToken cancellationToken)
    {
        if (!ConfirmNextPublish)
        {
            return Task.FromResult(false);
        }

        _publishedIds.Add(reportId);
        return Task.FromResult(true);
    }
}

