using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Infrastructure.Execution;
using NodeControl.Infrastructure.Persistence;
using NodeControl.Infrastructure.Security;
using NodeControl.Infrastructure.Time;

namespace NodeControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNodeControlInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NodeControl")
            ?? "Host=localhost;Port=5432;Database=nodecontrol;Username=nodecontrol;Password=nodecontrol_dev_password";

        services.AddDbContext<NodeControlDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<INodeControlDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<NodeControlDbContext>());

        services.AddDataProtection();
        services.TryAddScoped<ISecretProtector, DataProtectionSecretProtector>();

        var executionOptions = new ExecutionOptions();
        var executionSection = configuration.GetSection(ExecutionOptions.SectionName);
        if (!string.IsNullOrWhiteSpace(executionSection[nameof(ExecutionOptions.RunWorkspaceRoot)]))
        {
            executionOptions.RunWorkspaceRoot = executionSection[nameof(ExecutionOptions.RunWorkspaceRoot)]!;
        }

        if (!string.IsNullOrWhiteSpace(executionSection[nameof(ExecutionOptions.AnsiblePlaybookPath)]))
        {
            executionOptions.AnsiblePlaybookPath = executionSection[nameof(ExecutionOptions.AnsiblePlaybookPath)]!;
        }

        services.AddSingleton(Options.Create(executionOptions));

        services.AddSingleton<IClock, SystemClock>();
        services.TryAddScoped<IRequestAuditContext, EmptyRequestAuditContext>();
        services.TryAddScoped<IAuditLogWriter, AuditLogWriter>();

        return services;
    }
}
