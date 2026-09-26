using System;

namespace ExpenseHub.UnitTests.Fakes;

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    // utc nos dois lados deixa "hoje" igual em qualquer máquina que rodar o teste
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() => _now;
}
