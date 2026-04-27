using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Application.Secrets;
using NodeControl.Application.Templates;
using NodeControl.Domain.Audit;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Templates;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class TemplatesServiceTests
{
    [Fact]
    public async Task TemplateService_creates_template_when_user_has_manage_templates()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateTemplateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("nginx-config"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Templates);
        Assert.Equal("nginx-config", result.Value!.Slug);
        Assert.Equal("template.created", fixture.Db.AuditLogEntries.Single().Action);
    }

    [Fact]
    public async Task TemplateService_rejects_create_when_user_lacks_manage_templates()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreateTemplateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("nginx-config"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.Templates);
    }

    [Fact]
    public async Task TemplateService_rejects_duplicate_slug_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateTemplateService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("nginx-config"));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("nginx-config"));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task TemplateService_allows_same_slug_in_different_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var service = fixture.CreateTemplateService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("nginx-config"));
        var second = await service.CreateAsync(other.CurrentUser, other.Customer.Id, ValidCreateRequest("nginx-config"));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.Templates.Count);
    }

    [Fact]
    public async Task TemplateService_rejects_invalid_slug()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateTemplateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("Bad Slug"));

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task TemplateService_rejects_empty_content()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateTemplateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("empty") with { Content = "   " });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task TemplateService_rejects_too_long_content()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateTemplateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("too-long") with { Content = new string('x', 200001) });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task TemplateService_gets_and_lists_templates_when_user_has_view_templates()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var template = Template.Create(
            fixture.Customer.Id,
            "Config",
            "config",
            null,
            TemplateType.ConfigFile,
            "server_name {{ host }};",
            "nginx",
            TestTime);
        fixture.Db.AddTemplate(template);

        var list = await fixture.CreateTemplateService().ListAsync(fixture.CurrentUser, fixture.Customer.Id);
        var get = await fixture.CreateTemplateService().GetAsync(fixture.CurrentUser, fixture.Customer.Id, template.Id);

        Assert.Null(list.Error);
        Assert.Single(list.Value!);
        Assert.Null(get.Error);
        Assert.Equal(template.Id, get.Value!.Id);
    }

    [Fact]
    public async Task TemplateService_rejects_get_and_list_when_user_lacks_view_templates()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);

        var list = await fixture.CreateTemplateService().ListAsync(fixture.CurrentUser, fixture.Customer.Id);
        var get = await fixture.CreateTemplateService().GetAsync(fixture.CurrentUser, fixture.Customer.Id, Guid.NewGuid());

        Assert.Equal(CustomerServiceError.Forbidden, list.Error);
        Assert.Equal(CustomerServiceError.Forbidden, get.Error);
    }

    [Fact]
    public async Task TemplateService_updates_template_when_user_has_manage_templates()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateTemplateService();
        var created = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("config"));

        var result = await service.UpdateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            created.Value!.Id,
            new UpdateTemplateRequest("Config 2", "config-2", null, "ConfigFile", "worker_processes auto;", "nginx"));

        Assert.Null(result.Error);
        Assert.Equal("config-2", fixture.Db.Templates.Single().Slug);
        Assert.Contains(fixture.Db.AuditLogEntries, entry => entry.Action == "template.updated");
    }

    [Fact]
    public async Task TemplateService_archives_instead_of_hard_deleting_and_excludes_from_default_list()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateTemplateService();
        var created = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("config"));

        var archived = await service.ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, created.Value!.Id);
        var list = await service.ListAsync(fixture.CurrentUser, fixture.Customer.Id);

        Assert.Null(archived.Error);
        Assert.Single(fixture.Db.Templates);
        Assert.Equal(TemplateStatus.Archived, fixture.Db.Templates[0].Status);
        Assert.Empty(list.Value!);
        Assert.Contains(fixture.Db.AuditLogEntries, entry => entry.Action == "template.archived");
    }

    [Fact]
    public async Task TemplateService_rejects_cross_tenant_get_update_and_archive()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var template = Template.Create(
            other.Customer.Id,
            "Config",
            "config",
            null,
            TemplateType.ConfigFile,
            "worker_processes auto;",
            "nginx",
            TestTime);
        fixture.Db.AddTemplate(template);
        var service = fixture.CreateTemplateService();

        var get = await service.GetAsync(fixture.CurrentUser, fixture.Customer.Id, template.Id);
        var update = await service.UpdateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            template.Id,
            new UpdateTemplateRequest("Config", "config", null, "ConfigFile", "x", "nginx"));
        var archive = await service.ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, template.Id);

        Assert.Equal(CustomerServiceError.NotFound, get.Error);
        Assert.Equal(CustomerServiceError.NotFound, update.Error);
        Assert.Equal(CustomerServiceError.NotFound, archive.Error);
    }

    [Fact]
    public void TemplateValidation_valid_generic_text_passes()
    {
        var result = new TemplateValidationService().Validate(TemplateType.GenericText, "hello", null);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void TemplateValidation_empty_content_fails()
    {
        var result = new TemplateValidationService().Validate(TemplateType.GenericText, " ", null);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TemplateValidation_balanced_jinja2_passes()
    {
        var result = new TemplateValidationService().Validate(TemplateType.Jinja2, "Hello {{ name }}{% if enabled %} yes {% endif %}", null);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Hello {{ name")]
    [InlineData("{% if enabled")]
    public void TemplateValidation_unclosed_jinja2_delimiter_fails(string content)
    {
        var result = new TemplateValidationService().Validate(TemplateType.Jinja2, content, null);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void TemplateValidation_shell_script_is_not_executed()
    {
        var result = new TemplateValidationService().Validate(
            TemplateType.ShellScript,
            "#!/bin/sh\nexit 99\n",
            "sh");

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task TemplateService_validate_request_does_not_persist_template()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateTemplateService().ValidateRequestAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            new ValidateTemplateRequest("Jinja2", "Hello {{ name }}", "jinja2"));

        Assert.Null(result.Error);
        Assert.True(result.Value!.IsValid);
        Assert.Empty(fixture.Db.Templates);
    }

    [Fact]
    public async Task TemplateValidation_reports_missing_secret_reference_as_invalid()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateTemplateService().ValidateRequestAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            new ValidateTemplateRequest("Jinja2", "password: secret://missing-secret", "jinja2"));

        Assert.Null(result.Error);
        Assert.False(result.Value!.IsValid);
        Assert.Single(result.Value.SecretReferences);
        Assert.False(result.Value.SecretReferences[0].Found);
    }

    [Fact]
    public async Task TemplateValidation_accepts_active_secret_reference()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        fixture.Db.AddSecret(Secret.Create(fixture.Customer.Id, "DB Password", "db-password", null, SecretKind.Password, "protected", TestTime));

        var result = await fixture.CreateTemplateService().ValidateRequestAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            new ValidateTemplateRequest("Jinja2", "password: secret://db-password", "jinja2"));

        Assert.Null(result.Error);
        Assert.True(result.Value!.IsValid);
        Assert.True(result.Value.SecretReferences[0].Found);
    }

    [Fact]
    public async Task Template_audit_metadata_does_not_contain_full_content()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        await fixture.CreateTemplateService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("config") with { Content = "unique-content-that-must-not-be-audited" });

        var metadata = fixture.Db.AuditLogEntries.Single().MetadataJson;
        Assert.DoesNotContain("unique-content-that-must-not-be-audited", metadata);
        Assert.Contains("contentLength", metadata);
    }

    private static CreateTemplateRequest ValidCreateRequest(string slug)
    {
        return new CreateTemplateRequest(
            "Nginx Config",
            slug,
            null,
            "ConfigFile",
            "server_name {{ host }};",
            "nginx");
    }

    private sealed record CustomerUser(Customer Customer, CurrentUserDto CurrentUser);

    private sealed class TestFixture
    {
        private readonly TestClock clock = new();

        private TestFixture(NodeControlTestDbContext db, Customer customer, CurrentUserDto currentUser)
        {
            Db = db;
            Customer = customer;
            CurrentUser = currentUser;
        }

        public NodeControlTestDbContext Db { get; }

        public Customer Customer { get; }

        public CurrentUserDto CurrentUser { get; }

        public static TestFixture Create(CustomerRole role)
        {
            var db = new NodeControlTestDbContext();
            var user = User.Create("Test User", "test@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer A", "customer-a", null, TestTime);
            db.AddUser(user);
            db.AddCustomer(customer);
            db.AddCustomerMembership(CustomerMembership.Create(customer, user, role, TestTime));

            return new TestFixture(db, customer, CurrentUser(user));
        }

        public CustomerUser AddOtherCustomer()
        {
            var user = User.Create("Other User", "other@nodecontrol.local", false, TestTime);
            var customer = Customer.Create("Customer B", "customer-b", null, TestTime);
            Db.AddUser(user);
            Db.AddCustomer(customer);
            Db.AddCustomerMembership(CustomerMembership.Create(customer, user, CustomerRole.Owner, TestTime));
            return new CustomerUser(customer, CurrentUser(user));
        }

        public TemplateService CreateTemplateService()
        {
            return new TemplateService(
                Db,
                new CustomerAuthorizationService(Db),
                new TemplateValidationService(),
                new SecretReferenceValidationService(Db, new SecretReferenceParser()),
                clock,
                new AuditLogWriter(Db, clock, new EmptyRequestAuditContext()));
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = TestTime;
    }

    private static DateTimeOffset TestTime => new(2026, 4, 27, 10, 0, 0, TimeSpan.Zero);

    private static CurrentUserDto CurrentUser(User user)
    {
        return new CurrentUserDto(
            user.Id,
            user.DisplayName,
            user.Email,
            user.IsActive,
            user.IsPlatformAdmin,
            "fake",
            user.Id.ToString());
    }
}
