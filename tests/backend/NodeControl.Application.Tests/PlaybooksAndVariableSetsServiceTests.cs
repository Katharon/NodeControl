using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Application.Playbooks;
using NodeControl.Application.Secrets;
using NodeControl.Application.Validation;
using NodeControl.Application.VariableSets;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Playbooks;
using NodeControl.Domain.Users;
using NodeControl.Domain.VariableSets;

namespace NodeControl.Application.Tests;

public sealed class PlaybooksAndVariableSetsServiceTests
{
    [Fact]
    public async Task PlaybookService_creates_inline_yaml_when_user_has_manage_playbooks()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidPlaybookRequest("deploy-app"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Playbooks);
        Assert.Equal("site.yml", result.Value!.EntryFilePath);
    }

    [Fact]
    public async Task PlaybookService_rejects_create_when_user_only_has_view_playbooks()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidPlaybookRequest("deploy-app"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.Playbooks);
    }

    [Fact]
    public async Task PlaybookService_creates_artifact_directory_when_entry_file_exists()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidArtifactPlaybookRequest("deploy-app"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Playbooks);
        Assert.Equal(PlaybookSourceType.ArtifactDirectory, result.Value!.SourceType);
        Assert.Null(result.Value.InlineContent);
        Assert.Equal("site.yml", result.Value.EntryFilePath);
        Assert.Equal(2, result.Value.ArtifactFiles.Count);
    }

    [Fact]
    public async Task PlaybookService_rejects_artifact_directory_without_entry_file()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidArtifactPlaybookRequest("deploy-app") with { EntryFilePath = "missing.yml" });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task PlaybookService_rejects_artifact_directory_with_unsafe_paths()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidArtifactPlaybookRequest("deploy-app") with
            {
                ArtifactFiles =
                [
                    new PlaybookArtifactFileDto("site.yml", "- hosts: all\n"),
                    new PlaybookArtifactFileDto("../roles/app/tasks/main.yml", "- debug:\n    msg: hello\n")
                ]
            });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
        Assert.Empty(fixture.Db.Playbooks);
    }

    [Fact]
    public async Task PlaybookService_rejects_artifact_directory_with_duplicate_normalized_paths()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidArtifactPlaybookRequest("deploy-app") with
            {
                ArtifactFiles =
                [
                    new PlaybookArtifactFileDto("site.yml", "- hosts: all\n"),
                    new PlaybookArtifactFileDto("roles/app/tasks/main.yml", "- debug:\n    msg: hello\n"),
                    new PlaybookArtifactFileDto("roles\\app\\tasks\\main.yml", "- debug:\n    msg: duplicate\n")
                ]
            });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
        Assert.Empty(fixture.Db.Playbooks);
    }

    [Theory]
    [InlineData(PlaybookSourceType.GitRepository)]
    public async Task PlaybookService_rejects_reserved_source_types(PlaybookSourceType sourceType)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidPlaybookRequest("deploy-app") with { SourceType = sourceType });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task PlaybookService_archives_instead_of_hard_deleting()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var create = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidPlaybookRequest("deploy-app"));

        var result = await fixture.CreatePlaybookService().ArchiveAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            create.Value!.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Playbooks);
        Assert.Equal(PlaybookStatus.Archived, fixture.Db.Playbooks[0].Status);
    }

    [Fact]
    public async Task PlaybookService_rejects_duplicate_slug_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreatePlaybookService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidPlaybookRequest("deploy-app"));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidPlaybookRequest("deploy-app"));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task PlaybookService_allows_same_slug_in_different_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var service = fixture.CreatePlaybookService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidPlaybookRequest("deploy-app"));
        var second = await service.CreateAsync(other.CurrentUser, other.Customer.Id, ValidPlaybookRequest("deploy-app"));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.Playbooks.Count);
    }

    [Fact]
    public async Task PlaybookService_rejects_invalid_yaml()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreatePlaybookService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidPlaybookRequest("bad-yaml") with { InlineContent = "tasks: [broken" });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task VariableSetService_creates_yaml_when_yaml_is_valid()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateVariableSetService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidVariableSetRequest("defaults", VariableSetFormat.Yaml));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.VariableSets);
    }

    [Fact]
    public async Task VariableSetService_creates_json_when_json_is_valid()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateVariableSetService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidVariableSetRequest("json-vars", VariableSetFormat.Json));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.VariableSets);
    }

    [Theory]
    [InlineData(VariableSetFormat.Yaml, "vars: [broken")]
    [InlineData(VariableSetFormat.Json, "{ bad json")]
    public async Task VariableSetService_rejects_invalid_content(VariableSetFormat format, string content)
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateVariableSetService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidVariableSetRequest("bad-vars", format) with { Content = content });

        Assert.Equal(CustomerServiceError.BadRequest, result.Error);
    }

    [Fact]
    public async Task VariableSetService_rejects_duplicate_slug_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateVariableSetService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidVariableSetRequest("defaults", VariableSetFormat.Yaml));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidVariableSetRequest("defaults", VariableSetFormat.Yaml));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task VariableSetService_allows_same_slug_in_different_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var service = fixture.CreateVariableSetService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidVariableSetRequest("defaults", VariableSetFormat.Yaml));
        var second = await service.CreateAsync(other.CurrentUser, other.Customer.Id, ValidVariableSetRequest("defaults", VariableSetFormat.Yaml));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.VariableSets.Count);
    }

    [Fact]
    public async Task VariableSetService_archives_instead_of_hard_deleting()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var create = await fixture.CreateVariableSetService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidVariableSetRequest("defaults", VariableSetFormat.Yaml));

        var result = await fixture.CreateVariableSetService().ArchiveAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            create.Value!.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.VariableSets);
        Assert.Equal(VariableSetStatus.Archived, fixture.Db.VariableSets[0].Status);
    }

    private static CreatePlaybookRequest ValidPlaybookRequest(string slug)
    {
        return new CreatePlaybookRequest(
            "Deploy App",
            slug,
            null,
            PlaybookSourceType.InlineYaml,
            "- hosts: all\n  tasks:\n    - debug:\n        msg: hello\n",
            null);
    }

    private static CreatePlaybookRequest ValidArtifactPlaybookRequest(string slug)
    {
        return new CreatePlaybookRequest(
            "Deploy App",
            slug,
            null,
            PlaybookSourceType.ArtifactDirectory,
            null,
            "site.yml",
            [
                new PlaybookArtifactFileDto("site.yml", "- hosts: all\n  roles:\n    - app\n"),
                new PlaybookArtifactFileDto("roles/app/tasks/main.yml", "- debug:\n    msg: hello\n")
            ]);
    }

    private static CreateVariableSetRequest ValidVariableSetRequest(string slug, VariableSetFormat format)
    {
        var content = format == VariableSetFormat.Json
            ? "{\"packageName\":\"nginx\"}"
            : "package_name: nginx\n";
        return new CreateVariableSetRequest("Defaults", slug, null, format, content, false);
    }

    private sealed record CustomerUser(Customer Customer, CurrentUserDto CurrentUser);

    private sealed class TestFixture
    {
        private readonly TestClock clock = new();
        private readonly YamlJsonValidationService validationService = new();

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

        public PlaybookService CreatePlaybookService()
        {
            return new PlaybookService(Db, new CustomerAuthorizationService(Db), validationService, clock);
        }

        public VariableSetService CreateVariableSetService()
        {
            return new VariableSetService(
                Db,
                new CustomerAuthorizationService(Db),
                validationService,
                new SecretReferenceValidationService(Db, new SecretReferenceParser()),
                clock);
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
