namespace NodeControl.Application.Abstractions.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
