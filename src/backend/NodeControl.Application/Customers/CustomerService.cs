using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Domain.Authorization;
using NodeControl.Domain.Customers;

namespace NodeControl.Application.Customers;

public sealed class CustomerService(
    INodeControlDbContext dbContext,
    ICustomerAuthorizationService authorizationService,
    IClock clock)
{
    public async Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(
        CurrentUserDto currentUser,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsActive)
        {
            return [];
        }

        var customers = currentUser.IsPlatformAdmin
            ? await dbContext.ListActiveCustomersAsync(cancellationToken)
            : await dbContext.ListActiveCustomersForUserAsync(currentUser.Id, cancellationToken);

        if (currentUser.IsPlatformAdmin)
        {
            return customers.Select(customer => MapCustomer(customer, currentUser, null)).ToArray();
        }

        var customerDtos = new List<CustomerDto>(customers.Count);
        foreach (var customer in customers)
        {
            var membership = await dbContext.FindCustomerMembershipForUserAsync(
                customer.Id,
                currentUser.Id,
                cancellationToken);
            customerDtos.Add(MapCustomer(customer, currentUser, membership));
        }

        return customerDtos;
    }

    public async Task<CustomerServiceResult<CustomerDto>> GetCustomerAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ViewCustomer,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<CustomerDto>.FromAuthorization(authorization);
        }

        var customer = await dbContext.FindCustomerAsync(customerId, cancellationToken);
        return customer is null
            ? CustomerServiceResult<CustomerDto>.NotFound()
            : CustomerServiceResult<CustomerDto>.Ok(await MapCustomerAsync(customer, currentUser, cancellationToken));
    }

    public async Task<CustomerServiceResult<CustomerDto>> CreateCustomerAsync(
        CurrentUserDto currentUser,
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsActive || !currentUser.IsPlatformAdmin)
        {
            return CustomerServiceResult<CustomerDto>.Forbidden();
        }

        var now = clock.UtcNow;
        var customer = Customer.Create(request.Name, request.Slug, request.Description, now);
        var user = await dbContext.FindUserAsync(currentUser.Id, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return CustomerServiceResult<CustomerDto>.Forbidden();
        }

        var membership = CustomerMembership.Create(customer, user, CustomerRole.Owner, now);

        dbContext.AddCustomer(customer);
        dbContext.AddCustomerMembership(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<CustomerDto>.Ok(MapCustomer(customer, currentUser, membership));
    }

    public async Task<CustomerServiceResult<CustomerDto>> UpdateCustomerAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageCustomer,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<CustomerDto>.FromAuthorization(authorization);
        }

        var customer = await dbContext.FindCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return CustomerServiceResult<CustomerDto>.NotFound();
        }

        customer.Update(request.Name, request.Slug, request.Description, clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<CustomerDto>.Ok(await MapCustomerAsync(customer, currentUser, cancellationToken));
    }

    public async Task<CustomerServiceResult<CustomerDto>> ArchiveCustomerAsync(
        CurrentUserDto currentUser,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(
            currentUser,
            customerId,
            Permission.ManageCustomer,
            cancellationToken);

        if (authorization != CustomerAuthorizationResult.Allowed)
        {
            return CustomerServiceResult<CustomerDto>.FromAuthorization(authorization);
        }

        var customer = await dbContext.FindCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return CustomerServiceResult<CustomerDto>.NotFound();
        }

        customer.Archive(clock.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CustomerServiceResult<CustomerDto>.Ok(await MapCustomerAsync(customer, currentUser, cancellationToken));
    }

    private static CustomerDto MapCustomer(
        Customer customer,
        CurrentUserDto currentUser,
        CustomerMembership? knownMembership)
    {
        var permissions = Enum.GetValues<Permission>();
        if (!currentUser.IsPlatformAdmin)
        {
            permissions = knownMembership is { IsActive: true }
                ? RolePermissionMap.GetPermissions(knownMembership.Role).ToArray()
                : [];
        }

        return new CustomerDto(
            customer.Id,
            customer.Name,
            customer.Slug,
            customer.Description,
            customer.Status,
            customer.CreatedAt,
            customer.UpdatedAt,
            customer.ArchivedAt,
            permissions);
    }

    private async Task<CustomerDto> MapCustomerAsync(
        Customer customer,
        CurrentUserDto currentUser,
        CancellationToken cancellationToken)
    {
        if (currentUser.IsPlatformAdmin)
        {
            return MapCustomer(customer, currentUser, null);
        }

        var membership = await dbContext.FindCustomerMembershipForUserAsync(
            customer.Id,
            currentUser.Id,
            cancellationToken);
        return MapCustomer(customer, currentUser, membership);
    }
}

public sealed record CustomerServiceResult<T>(T? Value, CustomerServiceError? Error)
{
    public static CustomerServiceResult<T> Ok(T value)
    {
        return new CustomerServiceResult<T>(value, null);
    }

    public static CustomerServiceResult<T> NotFound()
    {
        return new CustomerServiceResult<T>(default, CustomerServiceError.NotFound);
    }

    public static CustomerServiceResult<T> Forbidden()
    {
        return new CustomerServiceResult<T>(default, CustomerServiceError.Forbidden);
    }

    public static CustomerServiceResult<T> Conflict()
    {
        return new CustomerServiceResult<T>(default, CustomerServiceError.Conflict);
    }

    public static CustomerServiceResult<T> FromAuthorization(CustomerAuthorizationResult authorization)
    {
        return authorization switch
        {
            CustomerAuthorizationResult.NotFound => NotFound(),
            CustomerAuthorizationResult.Forbidden => Forbidden(),
            _ => throw new InvalidOperationException("Allowed authorization cannot be mapped to a failure.")
        };
    }
}

public enum CustomerServiceError
{
    NotFound,
    Forbidden,
    Conflict
}
