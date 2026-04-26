using NodeControl.Application.Auth;
using NodeControl.Application.Customers;

namespace NodeControl.Api.Endpoints;

public static class CustomersEndpoints
{
    public static IEndpointRouteBuilder MapCustomersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers")
            .RequireAuthorization();

        group.MapGet("/", async (
            CurrentUserService currentUserService,
            CustomerService customerService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var customers = await customerService.ListCustomersAsync(currentUser, cancellationToken);
            return Results.Ok(customers);
        });

        group.MapPost("/", async (
            CreateCustomerRequest request,
            CurrentUserService currentUserService,
            CustomerService customerService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await customerService.CreateCustomerAsync(currentUser, request, cancellationToken);
            return ToResult(result, createdPath: customer => $"/api/v1/customers/{customer.Id}");
        });

        group.MapGet("/{customerId:guid}", async (
            Guid customerId,
            CurrentUserService currentUserService,
            CustomerService customerService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await customerService.GetCustomerAsync(currentUser, customerId, cancellationToken);
            return ToResult(result);
        });

        group.MapPut("/{customerId:guid}", async (
            Guid customerId,
            UpdateCustomerRequest request,
            CurrentUserService currentUserService,
            CustomerService customerService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await customerService.UpdateCustomerAsync(currentUser, customerId, request, cancellationToken);
            return ToResult(result);
        });

        group.MapDelete("/{customerId:guid}", async (
            Guid customerId,
            CurrentUserService currentUserService,
            CustomerService customerService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await customerService.ArchiveCustomerAsync(currentUser, customerId, cancellationToken);
            return ToResult(result);
        });

        return endpoints;
    }

    internal static IResult ToResult<T>(
        CustomerServiceResult<T> result,
        Func<T, string>? createdPath = null)
    {
        if (result.Value is not null)
        {
            return createdPath is null
                ? Results.Ok(result.Value)
                : Results.Created(createdPath(result.Value), result.Value);
        }

        return result.Error switch
        {
            CustomerServiceError.Forbidden => Results.Forbid(),
            CustomerServiceError.Conflict => Results.Conflict(),
            CustomerServiceError.NotFound => Results.NotFound(),
            CustomerServiceError.BadRequest => Results.BadRequest(),
            _ => Results.BadRequest()
        };
    }
}
