using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
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

        var dataProtectionOptions = configuration
            .GetSection(NodeControlDataProtectionOptions.SectionName)
            .Get<NodeControlDataProtectionOptions>()
            ?? new NodeControlDataProtectionOptions();

        var keyRingDirectory = new DirectoryInfo(NormalizeDataProtectionKeyRingPath(dataProtectionOptions.KeyRingPath));
        Directory.CreateDirectory(keyRingDirectory.FullName);

        services.AddDataProtection()
            .SetApplicationName(NormalizeDataProtectionApplicationName(dataProtectionOptions.ApplicationName))
            .PersistKeysToFileSystem(keyRingDirectory);
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

        if (!string.IsNullOrWhiteSpace(executionSection[nameof(ExecutionOptions.RemoteAnsiblePlaybookPath)]))
        {
            executionOptions.RemoteAnsiblePlaybookPath = executionSection[nameof(ExecutionOptions.RemoteAnsiblePlaybookPath)]!;
        }

        if (bool.TryParse(executionSection[nameof(ExecutionOptions.AllowLocalControlNodeExecution)], out var allowLocalExecution))
        {
            executionOptions.AllowLocalControlNodeExecution = allowLocalExecution;
        }

        var localHostnames = executionSection.GetSection(nameof(ExecutionOptions.LocalControlNodeHostnames)).Get<string[]>();
        if (localHostnames is { Length: > 0 })
        {
            executionOptions.LocalControlNodeHostnames = localHostnames;
        }

        services.AddSingleton(Options.Create(executionOptions));

        services.AddSingleton<IClock, SystemClock>();
        services.TryAddScoped<IRequestAuditContext, EmptyRequestAuditContext>();
        services.TryAddScoped<IAuditLogWriter, AuditLogWriter>();

        return services;
    }

    private static string NormalizeDataProtectionApplicationName(string? applicationName)
    {
        return string.IsNullOrWhiteSpace(applicationName)
            ? "NodeControl"
            : applicationName.Trim();
    }

    private static string NormalizeDataProtectionKeyRingPath(string? keyRingPath)
    {
        var path = string.IsNullOrWhiteSpace(keyRingPath)
            ? new NodeControlDataProtectionOptions().KeyRingPath
            : keyRingPath.Trim();

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var repositoryRoot = TryFindRepositoryRoot(Directory.GetCurrentDirectory())
            ?? TryFindRepositoryRoot(AppContext.BaseDirectory);
        return Path.GetFullPath(Path.Combine(repositoryRoot ?? Directory.GetCurrentDirectory(), path));
    }

    private static string? TryFindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "backend", "NodeControl.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
