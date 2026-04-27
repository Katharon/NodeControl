using Microsoft.Extensions.Options;
using NodeControl.Application.Abstractions.Execution;
using NodeControl.Application.JobRuns;
using NodeControl.Application.Schedules;
using NodeControl.Infrastructure;
using NodeControl.Infrastructure.Execution;
using NodeControl.Worker.JobRuns;
using NodeControl.Worker.Schedules;

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
        services.AddSingleton<ICronScheduleCalculator, CronScheduleCalculator>();
        services.AddScoped<JobRunExecutionService>();
        services.AddScoped<ScheduledJobRunService>();
        services.AddHostedService<ScheduledJobRunWorker>();
        services.AddHostedService<QueuedJobRunWorker>();

        return services;
    }
}
