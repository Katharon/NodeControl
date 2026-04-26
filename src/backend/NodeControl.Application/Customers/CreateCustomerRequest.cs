namespace NodeControl.Application.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string Slug,
    string? Description);
