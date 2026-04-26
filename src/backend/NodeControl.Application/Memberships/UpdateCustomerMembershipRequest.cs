using NodeControl.Domain.Customers;

namespace NodeControl.Application.Memberships;

public sealed record UpdateCustomerMembershipRequest(
    CustomerRole Role,
    bool IsActive);
