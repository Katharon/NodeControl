using NodeControl.Application.Auth;
using NodeControl.Application.JobRuns;
using NodeControl.Application.Jobs;

namespace NodeControl.Api.Endpoints;

public static class JobsEndpoints
{
    public static IEndpointRouteBuilder MapJobsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/customers/{customerId:guid}/jobs")
            .RequireAuthorization();

        group.MapGet("/", async (
            Guid customerId,
            CurrentUserService currentUserService,
            JobService jobService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobService.ListAsync(currentUser, customerId, cancellationToken));
        });

        group.MapPost("/", async (
            Guid customerId,
            CreateJobRequest request,
            CurrentUserService currentUserService,
            JobService jobService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await jobService.CreateAsync(currentUser, customerId, request, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                job => $"/api/v1/customers/{customerId}/jobs/{job.Id}");
        });

        group.MapGet("/{jobId:guid}", async (
            Guid customerId,
            Guid jobId,
            CurrentUserService currentUserService,
            JobService jobService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobService.GetAsync(currentUser, customerId, jobId, cancellationToken));
        });

        group.MapPut("/{jobId:guid}", async (
            Guid customerId,
            Guid jobId,
            UpdateJobRequest request,
            CurrentUserService currentUserService,
            JobService jobService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobService.UpdateAsync(currentUser, customerId, jobId, request, cancellationToken));
        });

        group.MapDelete("/{jobId:guid}", async (
            Guid customerId,
            Guid jobId,
            CurrentUserService currentUserService,
            JobService jobService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            return currentUser is null
                ? Results.Unauthorized()
                : CustomersEndpoints.ToResult(await jobService.ArchiveAsync(currentUser, customerId, jobId, cancellationToken));
        });

        group.MapPost("/{jobId:guid}/run", async (
            Guid customerId,
            Guid jobId,
            CurrentUserService currentUserService,
            JobRunService jobRunService,
            CancellationToken cancellationToken) =>
        {
            var currentUser = await currentUserService.GetCurrentUserAsync(cancellationToken);
            if (currentUser is null)
            {
                return Results.Unauthorized();
            }

            var result = await jobRunService.CreateManualAsync(currentUser, customerId, jobId, cancellationToken);
            return CustomersEndpoints.ToResult(
                result,
                jobRun => $"/api/v1/customers/{customerId}/job-runs/{jobRun.Id}");
        });

        return endpoints;
    }
}
