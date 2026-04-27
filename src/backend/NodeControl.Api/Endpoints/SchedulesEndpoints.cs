using NodeControl.Application.Auth;
using NodeControl.Application.Schedules;

namespace NodeControl.Api.Endpoints;

public static class SchedulesEndpoints
{
    public static IEndpointRouteBuilder MapSchedulesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/schedules")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await scheduleService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateJobScheduleRequest request,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await scheduleService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                schedule => $"/api/v1/customers/{customerId}/schedules/{schedule.Id}");
        });

        group.MapGet("/{scheduleId:guid}", async (
            Guid customerId,
            Guid scheduleId,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await scheduleService.GetAsync(currentUser, customerId, scheduleId, cancellationToken));
        });

        group.MapPut("/{scheduleId:guid}", async (
            Guid customerId,
            Guid scheduleId,
            UpdateJobScheduleRequest request,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await scheduleService.UpdateAsync(currentUser, customerId, scheduleId, request, cancellationToken));
        });

        group.MapPost("/{scheduleId:guid}/pause", async (
            Guid customerId,
            Guid scheduleId,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await scheduleService.PauseAsync(currentUser, customerId, scheduleId, cancellationToken));
        });

        group.MapPost("/{scheduleId:guid}/resume", async (
            Guid customerId,
            Guid scheduleId,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await scheduleService.ResumeAsync(currentUser, customerId, scheduleId, cancellationToken));
        });

        group.MapDelete("/{scheduleId:guid}", async (
            Guid customerId,
            Guid scheduleId,
            CurrentUserService currentUserService,
            JobScheduleService scheduleService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await scheduleService.ArchiveAsync(currentUser, customerId, scheduleId, cancellationToken));
        });

        return endpoints;
    }
}
