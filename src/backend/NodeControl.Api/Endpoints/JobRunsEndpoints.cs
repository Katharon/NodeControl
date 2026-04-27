using NodeControl.Application.Auth;
using NodeControl.Application.JobRuns;

namespace NodeControl.Api.Endpoints;

public static class JobRunsEndpoints
{
    public static IEndpointRouteBuilder MapJobRunsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/job-runs")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            JobRunService jobRunService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobRunService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapGet("/{jobRunId:guid}", async (
            Guid customerId,
            Guid jobRunId,
            CurrentUserService currentUserService,
            JobRunService jobRunService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobRunService.GetAsync(currentUser, customerId, jobRunId, cancellationToken));
        });

        group.MapGet("/{jobRunId:guid}/logs", async (
            Guid customerId,
            Guid jobRunId,
            long? afterSequence,
            int? limit,
            CurrentUserService currentUserService,
            JobRunLogService jobRunLogService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobRunLogService.ListAsync(
                    currentUser,
                    customerId,
                    jobRunId,
                    afterSequence,
                    limit,
                    cancellationToken));
        });

        group.MapPost("/{jobRunId:guid}/cancel", async (
            Guid customerId,
            Guid jobRunId,
            CancelJobRunRequest? request,
            CurrentUserService currentUserService,
            JobRunService jobRunService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobRunService.CancelAsync(
                    currentUser,
                    customerId,
                    jobRunId,
                    request ?? new CancelJobRunRequest(null),
                    cancellationToken));
        });

        group.MapPost("/{jobRunId:guid}/retry", async (
            Guid customerId,
            Guid jobRunId,
            CurrentUserService currentUserService,
            JobRunService jobRunService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await jobRunService.RetryAsync(currentUser, customerId, jobRunId, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                jobRun => $"/api/v1/customers/{customerId}/job-runs/{jobRun.Id}");
        });

        return endpoints;
    }
}
