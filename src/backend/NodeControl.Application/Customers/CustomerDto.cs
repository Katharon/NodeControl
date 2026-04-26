using NodeControl.Domain.Authorization;
using NodeControl.Domain.Customers;

namespace NodeControl.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    CustomerStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt,
    IReadOnlyCollection<Permission> Permissions);
