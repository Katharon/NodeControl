using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Infrastructure;

namespace NodeControl.Application.Tests;

public sealed class DataProtectionSecretProtectorTests
{
    private static readonly object CurrentDirectoryLock = new();

    [Fact]
    public void Secret_protected_by_api_configuration_can_be_unprotected_by_worker_configuration()
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), "nodecontrol-data-protection-tests", Guid.NewGuid().ToString("N"));
        try
        {
            using var apiProvider = CreateProvider(keyRingPath, "NodeControl");
            using var workerProvider = CreateProvider(keyRingPath, "NodeControl");
            var apiProtector = apiProvider.GetRequiredService<ISecretProtector>();
            var workerProtector = workerProvider.GetRequiredService<ISecretProtector>();

            var protectedValue = apiProtector.Protect("-----BEGIN OPENSSH PRIVATE KEY-----\nkey\n-----END OPENSSH PRIVATE KEY-----");

            Assert.Equal(
                "-----BEGIN OPENSSH PRIVATE KEY-----\nkey\n-----END OPENSSH PRIVATE KEY-----",
                workerProtector.Unprotect(protectedValue));
            Assert.NotEmpty(Directory.GetFiles(keyRingPath, "*.xml"));
        }
        finally
        {
            if (Directory.Exists(keyRingPath))
            {
                Directory.Delete(keyRingPath, recursive: true);
            }
        }
    }

    [Fact]
    public void Relative_development_key_ring_path_is_stable_across_project_working_directories()
    {
        var keyRingRelativePath = $".nodecontrol/data-protection-test-{Guid.NewGuid():N}";
        var keyRingPath = Path.Combine(FindRepositoryRoot(), keyRingRelativePath);
        try
        {
            string protectedValue;
            lock (CurrentDirectoryLock)
            {
                var originalDirectory = Directory.GetCurrentDirectory();
                try
                {
                    Directory.SetCurrentDirectory(Path.Combine(FindRepositoryRoot(), "src", "backend", "NodeControl.Api"));
                    using var apiProvider = CreateProvider(keyRingRelativePath, "NodeControl");
                    protectedValue = apiProvider
                        .GetRequiredService<ISecretProtector>()
                        .Protect("ssh-private-key");

                    Directory.SetCurrentDirectory(Path.Combine(FindRepositoryRoot(), "src", "backend", "NodeControl.Worker"));
                    using var workerProvider = CreateProvider(keyRingRelativePath, "NodeControl");
                    Assert.Equal(
                        "ssh-private-key",
                        workerProvider.GetRequiredService<ISecretProtector>().Unprotect(protectedValue));
                }
                finally
                {
                    Directory.SetCurrentDirectory(originalDirectory);
                }
            }

            Assert.NotEmpty(Directory.GetFiles(keyRingPath, "*.xml"));
        }
        finally
        {
            if (Directory.Exists(keyRingPath))
            {
                Directory.Delete(keyRingPath, recursive: true);
            }
        }
    }

    private static ServiceProvider CreateProvider(string keyRingPath, string applicationName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NodeControl:DataProtection:ApplicationName"] = applicationName,
                ["NodeControl:DataProtection:KeyRingPath"] = keyRingPath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddNodeControlInfrastructure(configuration);
        return services.BuildServiceProvider();
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "backend", "NodeControl.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
