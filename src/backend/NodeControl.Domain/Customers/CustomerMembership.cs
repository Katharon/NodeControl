using NodeControl.Domain.Users;

namespace NodeControl.Domain.Customers;

public sealed class CustomerMembership
{
    private CustomerMembership()
    {
    }

    private CustomerMembership(
        Guid id,
        Customer customer,
        User user,
        CustomerRole role,
        DateTimeOffset createdAt)
    {
        Id = id;
        Customer = customer;
        CustomerId = customer.Id;
        User = user;
        UserId = user.Id;
        Role = role;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public CustomerRole Role { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public DateTimeOffset? DeactivatedAt { get; private set; }

    public static CustomerMembership Create(
        Customer customer,
        User user,
        CustomerRole role,
        DateTimeOffset createdAt)
    {
        return new CustomerMembership(Guid.NewGuid(), customer, user, role, createdAt);
    }

    public void Update(CustomerRole role, bool isActive, DateTimeOffset updatedAt)
    {
        Role = role;
        IsActive = isActive;
        UpdatedAt = updatedAt;
        DeactivatedAt = isActive ? null : updatedAt;
    }

    public void Deactivate(DateTimeOffset deactivatedAt)
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        DeactivatedAt = deactivatedAt;
        UpdatedAt = deactivatedAt;
    }
}
