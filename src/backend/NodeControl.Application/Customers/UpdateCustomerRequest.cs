namespace NodeControl.Application.Customers;

public sealed record UpdateCustomerRequest(
    string Name,
    string Slug,
    string? Description);
