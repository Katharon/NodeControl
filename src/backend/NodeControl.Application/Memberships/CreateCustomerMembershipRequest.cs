using NodeControl.Domain.Customers;

namespace NodeControl.Application.Memberships;

public sealed record CreateCustomerMembershipRequest(
    Guid UserId,
    CustomerRole Role);
