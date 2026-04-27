using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Templates;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Templates;
using NodeControl.Domain.Users;

namespace NodeControl.Api.IntegrationTests;

public sealed class TemplatesEndpointTests
{
    [Fact]
    public async Task Get_templates_requires_view_templates()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        factory.Db.AddTemplate(Template.Create(seeded.Customer.Id, "Config", "config", null, TemplateType.ConfigFile, "x", "nginx", TestTime));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Post_templates_requires_manage_templates()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates",
            ValidCreateRequest("config"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_template_by_id_requires_view_templates()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Auditor);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var template = Template.Create(seeded.Customer.Id, "Config", "config", null, TemplateType.ConfigFile, "x", "nginx", TestTime);
        factory.Db.AddTemplate(template);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/templates/{template.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Put_delete_and_validate_require_manage_templates()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Viewer);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var template = Template.Create(seeded.Customer.Id, "Config", "config", null, TemplateType.ConfigFile, "x", "nginx", TestTime);
        factory.Db.AddTemplate(template);
        using var client = factory.CreateClient();

        using var putResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates/{template.Id}",
            new UpdateTemplateRequest("Config", "config", null, "ConfigFile", "x", "nginx"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/templates/{template.Id}");
        using var validateResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates/validate",
            new ValidateTemplateRequest("ConfigFile", "x", "nginx"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, validateResponse.StatusCode);
    }

    [Fact]
    public async Task Manage_templates_can_create_update_archive_and_validate()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates",
            ValidCreateRequest("config"),
            JsonOptions);
        var template = await createResponse.Content.ReadFromJsonAsync<TemplateDto>(JsonOptions);
        using var validateStoredResponse = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates/{template!.Id}/validate",
            new { },
            JsonOptions);
        using var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates/{template.Id}",
            new UpdateTemplateRequest("Config 2", "config-2", null, "ConfigFile", "worker_processes auto;", "nginx"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/templates/{template.Id}");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, validateStoredResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_access_is_rejected_for_detail_update_and_archive()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        var otherTemplate = Template.Create(otherCustomer.Id, "Config", "config", null, TemplateType.ConfigFile, "x", "nginx", TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddTemplate(otherTemplate);
        using var client = factory.CreateClient();

        using var getResponse = await client.GetAsync($"/api/v1/customers/{seeded.Customer.Id}/templates/{otherTemplate.Id}");
        using var putResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates/{otherTemplate.Id}",
            new UpdateTemplateRequest("Config", "config", null, "ConfigFile", "x", "nginx"),
            JsonOptions);
        using var deleteResponse = await client.DeleteAsync($"/api/v1/customers/{seeded.Customer.Id}/templates/{otherTemplate.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task List_endpoint_does_not_return_templates_from_another_customer()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        var otherCustomer = Customer.Create("Other Customer", "other-customer", null, TestTime);
        factory.Db.AddCustomer(otherCustomer);
        factory.Db.AddTemplate(Template.Create(seeded.Customer.Id, "Config", "config", null, TemplateType.ConfigFile, "x", "nginx", TestTime));
        factory.Db.AddTemplate(Template.Create(otherCustomer.Id, "Other", "other", null, TemplateType.ConfigFile, "x", "nginx", TestTime));
        using var client = factory.CreateClient();

        var templates = await client.GetFromJsonAsync<TemplateDto[]>($"/api/v1/customers/{seeded.Customer.Id}/templates", JsonOptions);

        Assert.Single(templates!);
        Assert.Equal("config", templates![0].Slug);
    }

    [Fact]
    public async Task Validation_endpoint_does_not_persist_template()
    {
        await using var factory = TemplateApiFactory.Create(CustomerRole.Owner);
        var seeded = factory.SeedCurrentUserAndCustomer();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/v1/customers/{seeded.Customer.Id}/templates/validate",
            new ValidateTemplateRequest("Jinja2", "Hello {{ name }}", "jinja2"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(factory.Db.Templates);
    }

    private static CreateTemplateRequest ValidCreateRequest(string slug)
    {
        return new CreateTemplateRequest("Config", slug, null, "ConfigFile", "server_name {{ host }};", "nginx");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private sealed record Seeded(User User, Customer Customer);

    private sealed class TemplateApiFactory(
        CustomerRole role,
        bool isPlatformAdmin = false) : WebApplicationFactory<Program>
    {
        private const string Provider = "fake";
        private const string Subject = "template-user";
        private const string Email = "template-user@nodecontrol.local";
        private const string DisplayName = "Template User";

        public TemplateApiDbContext Db { get; } = new();

        public static TemplateApiFactory Create(CustomerRole role)
        {
            return new TemplateApiFactory(role);
        }

        public Seeded SeedCurrentUserAndCustomer()
        {
            var user = User.Create(DisplayName, Email, isPlatformAdmin, TestTime);
            var identity = ExternalIdentity.Create(user, Provider, Subject, Email, DisplayName, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            Db.AddUser(user);
            Db.AddExternalIdentity(identity);
            Db.AddCustomer(customer);
            if (!isPlatformAdmin)
            {
                Db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));
            }

            return new Seeded(user, customer);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Auth:Mode"] = "Fake",
                    ["Auth:Fake:Provider"] = Provider,
                    ["Auth:Fake:Subject"] = Subject,
                    ["Auth:Fake:Email"] = Email,
                    ["Auth:Fake:DisplayName"] = DisplayName,
                    ["Auth:Fake:IsPlatformAdmin"] = isPlatformAdmin.ToString()
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INodeControlDbContext>();
                services.AddSingleton<INodeControlDbContext>(Db);
            });
        }
    }

    private sealed class TemplateApiDbContext : INodeControlDbContext
    {
        private readonly object syncRoot = new();

        public List<User> Users { get; } = [];

        public List<ExternalIdentity> ExternalIdentities { get; } = [];

        public List<Customer> Customers { get; } = [];

        public List<CustomerMembership> CustomerMemberships { get; } = [];

        public List<Template> Templates { get; } = [];

        public List<AuditLogEntry> AuditLogEntries { get; } = [];

        public Task<ExternalIdentity?> FindExternalIdentityAsync(string provider, string subject, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(ExternalIdentities.FirstOrDefault(identity =>
                    identity.Provider == provider && identity.Subject == subject));
            }
        }

        public Task<User?> FindUserAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Users.FirstOrDefault(user => user.Id == id));
            }
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersAsync(CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Customer>>(
                    Customers.Where(customer => customer.Status == CustomerStatus.Active).ToArray());
            }
        }

        public Task<IReadOnlyList<Customer>> ListActiveCustomersForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Customer>>(
                    CustomerMemberships
                        .Where(membership => membership.UserId == userId
                            && membership.IsActive
                            && membership.Customer.Status == CustomerStatus.Active)
                        .Select(membership => membership.Customer)
                        .ToArray());
            }
        }

        public Task<Customer?> FindCustomerAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == id));
            }
        }

        public Task<IReadOnlyList<CustomerMembership>> ListCustomerMembershipsAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<CustomerMembership>>(
                    CustomerMemberships.Where(membership => membership.CustomerId == customerId).ToArray());
            }
        }

        public Task<CustomerMembership?> FindCustomerMembershipAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(CustomerMemberships.FirstOrDefault(membership => membership.Id == id));
            }
        }

        public Task<CustomerMembership?> FindCustomerMembershipForUserAsync(Guid customerId, Guid userId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(CustomerMemberships.FirstOrDefault(membership =>
                    membership.CustomerId == customerId && membership.UserId == userId));
            }
        }

        public Task<IReadOnlyList<Template>> ListActiveTemplatesAsync(Guid customerId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult<IReadOnlyList<Template>>(
                    Templates.Where(template => template.CustomerId == customerId && template.Status == TemplateStatus.Active).ToArray());
            }
        }

        public Task<Template?> FindTemplateAsync(Guid customerId, Guid templateId, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Templates.FirstOrDefault(template =>
                    template.CustomerId == customerId && template.Id == templateId));
            }
        }

        public Task<Template?> FindTemplateBySlugAsync(Guid customerId, string slug, CancellationToken cancellationToken)
        {
            lock (syncRoot)
            {
                return Task.FromResult(Templates.FirstOrDefault(template =>
                    template.CustomerId == customerId && template.Slug == slug));
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

        public void AddCustomer(Customer customer)
        {
            lock (syncRoot)
            {
                Customers.Add(customer);
            }
        }

        public void AddCustomerMembership(CustomerMembership customerMembership)
        {
            lock (syncRoot)
            {
                CustomerMemberships.Add(customerMembership);
            }
        }

        public void AddTemplate(Template template)
        {
            lock (syncRoot)
            {
                Templates.Add(template);
            }
        }

        public void AddAuditLogEntry(AuditLogEntry auditLogEntry)
        {
            lock (syncRoot)
            {
                AuditLogEntries.Add(auditLogEntry);
            }
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(1);
        }
    }
}
