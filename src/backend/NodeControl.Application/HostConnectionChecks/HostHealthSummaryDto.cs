using NodeControl.Domain.Nodes;

namespace NodeControl.Application.HostConnectionChecks;

public sealed record HostHealthSummaryDto(
    IReadOnlyList<HostHealthTargetDto> Targets);

public sealed record HostHealthTargetDto(
    HostConnectionTargetType TargetType,
    Guid TargetId,
    string Name,
    string Hostname,
    int Port,
    HostConnectionCheckDto? LatestCheck);
