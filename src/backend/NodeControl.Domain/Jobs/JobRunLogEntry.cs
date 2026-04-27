namespace NodeControl.Domain.Jobs;

public sealed class JobRunLogEntry
{
    private const int MessageMaxLength = 16000;

    private JobRunLogEntry()
    {
    }

    private JobRunLogEntry(
        Guid id,
        Guid jobRunId,
        long sequence,
        DateTimeOffset timestampUtc,
        JobRunLogStream stream,
        JobRunLogLevel level,
        string message)
    {
        Id = id;
        JobRunId = jobRunId;
        Sequence = sequence;
        TimestampUtc = timestampUtc;
        Stream = stream;
        Level = level;
        Message = NormalizeMessage(message);
    }

    public Guid Id { get; private set; }

    public Guid JobRunId { get; private set; }

    public JobRun JobRun { get; private set; } = null!;

    public long Sequence { get; private set; }

    public DateTimeOffset TimestampUtc { get; private set; }

    public JobRunLogStream Stream { get; private set; }

    public JobRunLogLevel Level { get; private set; }

    public string Message { get; private set; } = string.Empty;

    public static JobRunLogEntry Create(
        JobRun jobRun,
        long sequence,
        DateTimeOffset timestampUtc,
        JobRunLogStream stream,
        JobRunLogLevel level,
        string message)
    {
        ArgumentNullException.ThrowIfNull(jobRun);
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must start at 1.");
        }

        return new JobRunLogEntry(Guid.NewGuid(), jobRun.Id, sequence, timestampUtc, stream, level, message);
    }

    private static string NormalizeMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        var trimmed = message.Trim();
        return trimmed.Length <= MessageMaxLength ? trimmed : trimmed[..MessageMaxLength];
    }
}
