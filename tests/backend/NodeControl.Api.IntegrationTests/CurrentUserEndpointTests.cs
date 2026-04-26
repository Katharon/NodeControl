using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NodeControl.Api;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Domain.Users;

namespace NodeControl.Api.IntegrationTests;

public sealed class CurrentUserEndpointTests
{
    [Fact]
    public async Task Get_me_returns_200_with_fake_auth()
    {
        await using var factory = new AuthWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Dev Admin", document.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("dev-admin@nodecontrol.local", document.RootElement.GetProperty("email").GetString());
        Assert.Equal("fake", document.RootElement.GetProperty("authProvider").GetString());
        Assert.Equal("dev-admin", document.RootElement.GetProperty("externalSubject").GetString());
    }

    [Fact]
    public async Task Get_me_creates_and_reuses_internal_user()
    {
        await using var factory = new AuthWebApplicationFactory();
        using var client = factory.CreateClient();

        using var firstResponse = await client.GetAsync("/api/v1/me");
        using var secondResponse = await client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Single(factory.DbContext.Users);
        Assert.Single(factory.DbContext.ExternalIdentities);
    }

    [Fact]
    public void Fake_auth_cannot_be_used_in_production()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Mode"] = "Fake"
            })
            .Build();
        var services = new ServiceCollection();
        var environment = new TestWebHostEnvironment
        {
            EnvironmentName = Environments.Production
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddNodeControlApi(configuration, environment));
        Assert.Contains("Fake Auth cannot be enabled in Production", exception.Message);
    }

    private sealed class AuthWebApplicationFactory(string environment = "Development")
        : WebApplicationFactory<Program>
    {
        public TestNodeControlDbContext DbContext { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Mode"] = "Fake",
                    ["Auth:Fake:Provider"] = "fake",
                    ["Auth:Fake:Subject"] = "dev-admin",
                    ["Auth:Fake:Email"] = "dev-admin@nodecontrol.local",
                    ["Auth:Fake:DisplayName"] = "Dev Admin",
                    ["Auth:Fake:IsPlatformAdmin"] = "true"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INodeControlDbContext>();
                services.AddSingleton<INodeControlDbContext>(DbContext);
            });
        }
    }

    public sealed class TestNodeControlDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public Task<ExternalIdentity?> FindExternalIdentityAsync(
            string provider,
            string subject,
            CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ExternalIdentities.FirstOrDefault(externalIdentity =>
                    externalIdentity.Provider == provider && externalIdentity.Subject == subject));
            }
        }

        public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
            }
        }

        public void AddUser(User user)
        {
            lock (syncRoot)
            {
                Users.Add(user);
            }
        }

        public void AddExternalIdentity(ExternalIdentity externalIdentity)
        {
            lock (syncRoot)
            {
                ExternalIdentities.Add(externalIdentity);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "NodeControl.Api.Tests";

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

        public string EnvironmentName { get; set; } = Environments.Development;

        public string WebRootPath { get; set; } = Directory.GetCurrentDirectory();

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
