using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.JobRuns;
using NodeControl.Infrastructure;
using NodeControl.Infrastructure.Execution;
using NodeControl.Worker.JobRuns;

namespace NodeControl.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddNodeControlWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddNodeControlInfrastructure(configuration);
        services.AddScoped<IAnsiblePlaybookRunner, AnsiblePlaybookRunner>();
        services.AddScoped<IJobRunWorkspaceBuilder>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ExecutionOptions>>().Value;
            return new JobRunWorkspaceBuilder(options.RunWorkspaceRoot);
        });
        services.AddScoped<JobRunExecutionService>();
        services.AddHostedService<QueuedJobRunWorker>();

        return services;
    }
}
