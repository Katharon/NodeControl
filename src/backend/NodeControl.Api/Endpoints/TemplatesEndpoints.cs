using NodeControl.Application.Auth;
using NodeControl.Application.Templates;

namespace NodeControl.Api.Endpoints;

public static class TemplatesEndpoints
{
    public static IEndpointRouteBuilder MapTemplatesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/templates")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await templateService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateTemplateRequest request,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await templateService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                template => $"/api/v1/customers/{customerId}/templates/{template.Id}");
        });

        group.MapPost("/validate", async (
            Guid customerId,
            ValidateTemplateRequest request,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await templateService.ValidateRequestAsync(currentUser, customerId, request, cancellationToken));
        });

        group.MapGet("/{templateId:guid}", async (
            Guid customerId,
            Guid templateId,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await templateService.GetAsync(currentUser, customerId, templateId, cancellationToken));
        });

        group.MapPut("/{templateId:guid}", async (
            Guid customerId,
            Guid templateId,
            UpdateTemplateRequest request,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await templateService.UpdateAsync(currentUser, customerId, templateId, request, cancellationToken));
        });

        group.MapDelete("/{templateId:guid}", async (
            Guid customerId,
            Guid templateId,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await templateService.ArchiveAsync(currentUser, customerId, templateId, cancellationToken));
        });

        group.MapPost("/{templateId:guid}/validate", async (
            Guid customerId,
            Guid templateId,
            CurrentUserService currentUserService,
            TemplateService templateService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await templateService.ValidateExistingAsync(currentUser, customerId, templateId, cancellationToken));
        });

        return endpoints;
    }
}
