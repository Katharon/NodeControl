using NodeControl.Domain.Customers;

namespace NodeControl.Application.Memberships;

public sealed record CustomerMembershipDto(
    Guid Id,
    Guid CustomerId,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    CustomerRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? DeactivatedAt);
