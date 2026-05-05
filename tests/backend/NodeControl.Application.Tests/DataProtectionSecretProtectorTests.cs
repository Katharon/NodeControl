using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Infrastructure;

namespace NodeControl.Application.Tests;

public sealed class DataProtectionSecretProtectorTests
{
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
}
