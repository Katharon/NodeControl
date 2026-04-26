using NodeControl.Application.Abstractions.Time;

namespace NodeControl.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
