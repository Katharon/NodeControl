namespace NodeControl.Application.Users;

public sealed record UserListQuery(
    string? Query,
    bool IncludeInactive,
    int? Limit);
