using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Security;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Application.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Customers;
using NodeControl.Application.Secrets;
using NodeControl.Domain.Customers;
using NodeControl.Domain.Secrets;
using NodeControl.Domain.Users;

namespace NodeControl.Application.Tests;

public sealed class SecretsServiceTests
{
    [Fact]
    public void SecretReferenceParser_detects_one_reference()
    {
        var slugs = new SecretReferenceParser().ParseDistinctSlugs("password: secret://db-password");

        Assert.Equal(["db-password"], slugs);
    }

    [Fact]
    public void SecretReferenceParser_detects_multiple_distinct_references()
    {
        var slugs = new SecretReferenceParser().ParseDistinctSlugs("secret://api-token secret://db-password secret://api-token");

        Assert.Equal(["api-token", "db-password"], slugs);
    }

    [Fact]
    public void SecretReferenceParser_ignores_malformed_references()
    {
        var slugs = new SecretReferenceParser().ParseDistinctSlugs("secret://Bad secret://a secret://bad_slug");

        Assert.Empty(slugs);
    }

    [Fact]
    public async Task SecretReferenceValidation_accepts_active_same_customer_secret()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        fixture.Db.AddSecret(Secret.Create(fixture.Customer.Id, "DB Password", "db-password", null, SecretKind.Password, "protected", TestTime));

        var result = await fixture.CreateSecretReferenceValidationService().ValidateAsync(
            fixture.Customer.Id,
            "db_password: secret://db-password");

        Assert.True(result.IsValid);
        Assert.Single(result.References);
        Assert.True(result.References[0].Found);
    }

    [Fact]
    public async Task SecretReferenceValidation_rejects_missing_secret()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateSecretReferenceValidationService().ValidateAsync(
            fixture.Customer.Id,
            "db_password: secret://db-password");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("does not exist", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SecretReferenceValidation_rejects_archived_secret()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var secret = Secret.Create(fixture.Customer.Id, "DB Password", "db-password", null, SecretKind.Password, "protected", TestTime);
        secret.Archive(TestTime);
        fixture.Db.AddSecret(secret);

        var result = await fixture.CreateSecretReferenceValidationService().ValidateAsync(
            fixture.Customer.Id,
            "db_password: secret://db-password");

        Assert.False(result.IsValid);
        Assert.Equal("Archived", result.References[0].Status);
    }

    [Fact]
    public async Task SecretReferenceValidation_does_not_allow_cross_tenant_reference()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        fixture.Db.AddSecret(Secret.Create(other.Customer.Id, "DB Password", "db-password", null, SecretKind.Password, "protected", TestTime));

        var result = await fixture.CreateSecretReferenceValidationService().ValidateAsync(
            fixture.Customer.Id,
            "db_password: secret://db-password");

        Assert.False(result.IsValid);
        Assert.False(result.References[0].Found);
    }

    [Fact]
    public async Task SecretService_creates_secret_when_user_has_manage_secrets()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        var result = await fixture.CreateSecretService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("api-token", "super-secret-value"));

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Secrets);
        Assert.True(result.Value!.HasValue);
        Assert.NotEqual("super-secret-value", fixture.Db.Secrets.Single().ProtectedValue);
        Assert.Equal("secret.created", fixture.Db.AuditLogEntries.Single().Action);
    }

    [Fact]
    public async Task SecretService_rejects_create_without_manage_secrets()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);

        var result = await fixture.CreateSecretService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("api-token", "super-secret-value"));

        Assert.Equal(CustomerServiceError.Forbidden, result.Error);
        Assert.Empty(fixture.Db.Secrets);
    }

    [Fact]
    public async Task SecretService_lists_and_reads_with_view_secrets()
    {
        var fixture = TestFixture.Create(CustomerRole.Viewer);
        var secret = Secret.Create(
            fixture.Customer.Id,
            "API Token",
            "api-token",
            null,
            SecretKind.ApiToken,
            "protected-value",
            TestTime);
        fixture.Db.AddSecret(secret);

        var list = await fixture.CreateSecretService().ListAsync(fixture.CurrentUser, fixture.Customer.Id);
        var get = await fixture.CreateSecretService().GetAsync(fixture.CurrentUser, fixture.Customer.Id, secret.Id);

        Assert.Null(list.Error);
        Assert.Single(list.Value!);
        Assert.Null(get.Error);
        Assert.Equal(secret.Id, get.Value!.Id);
        Assert.True(get.Value.HasValue);
    }

    [Fact]
    public async Task SecretService_rejects_list_and_read_without_view_secrets()
    {
        var fixture = TestFixture.Create(CustomerRole.Auditor);

        var list = await fixture.CreateSecretService().ListAsync(fixture.CurrentUser, fixture.Customer.Id);
        var get = await fixture.CreateSecretService().GetAsync(fixture.CurrentUser, fixture.Customer.Id, Guid.NewGuid());

        Assert.Equal(CustomerServiceError.Forbidden, list.Error);
        Assert.Equal(CustomerServiceError.Forbidden, get.Error);
    }

    [Fact]
    public async Task SecretService_rejects_duplicate_slug_within_same_customer()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateSecretService();

        await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("api-token", "one"));
        var result = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("api-token", "two"));

        Assert.Equal(CustomerServiceError.Conflict, result.Error);
    }

    [Fact]
    public async Task SecretService_allows_same_slug_across_customers()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var service = fixture.CreateSecretService();

        var first = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("api-token", "one"));
        var second = await service.CreateAsync(other.CurrentUser, other.Customer.Id, ValidCreateRequest("api-token", "two"));

        Assert.Null(first.Error);
        Assert.Null(second.Error);
        Assert.Equal(2, fixture.Db.Secrets.Count);
    }

    [Fact]
    public async Task SecretService_rotate_changes_protected_value_and_last_rotated_at()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateSecretService();
        var created = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("api-token", "one"));
        var originalProtectedValue = fixture.Db.Secrets.Single().ProtectedValue;
        fixture.Clock.UtcNow = TestTime.AddHours(1);

        var result = await service.RotateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            created.Value!.Id,
            new RotateSecretRequest("two"));

        Assert.Null(result.Error);
        Assert.NotEqual(originalProtectedValue, fixture.Db.Secrets.Single().ProtectedValue);
        Assert.Equal(TestTime.AddHours(1), result.Value!.LastRotatedAtUtc);
        Assert.Contains(fixture.Db.AuditLogEntries, entry => entry.Action == "secret.rotated");
    }

    [Fact]
    public async Task SecretService_updates_metadata_without_resending_secret_value()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateSecretService();
        var created = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("api-token", "one"));
        var originalProtectedValue = fixture.Db.Secrets.Single().ProtectedValue;

        var result = await service.UpdateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            created.Value!.Id,
            new UpdateSecretRequest("API Token 2", "api-token-2", "updated", "ApiToken"));

        Assert.Null(result.Error);
        Assert.Equal(originalProtectedValue, fixture.Db.Secrets.Single().ProtectedValue);
        Assert.Equal("api-token-2", result.Value!.Slug);
    }

    [Fact]
    public async Task SecretService_archives_instead_of_hard_deleting()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var service = fixture.CreateSecretService();
        var created = await service.CreateAsync(fixture.CurrentUser, fixture.Customer.Id, ValidCreateRequest("api-token", "one"));

        var result = await service.ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, created.Value!.Id);

        Assert.Null(result.Error);
        Assert.Single(fixture.Db.Secrets);
        Assert.Equal(SecretStatus.Archived, fixture.Db.Secrets.Single().Status);
        Assert.NotNull(fixture.Db.Secrets.Single().ArchivedAt);
    }

    [Fact]
    public async Task SecretService_rejects_cross_tenant_read_update_rotate_and_archive()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);
        var other = fixture.AddOtherCustomer();
        var secret = Secret.Create(
            other.Customer.Id,
            "API Token",
            "api-token",
            null,
            SecretKind.ApiToken,
            "protected-value",
            TestTime);
        fixture.Db.AddSecret(secret);
        var service = fixture.CreateSecretService();

        var get = await service.GetAsync(fixture.CurrentUser, fixture.Customer.Id, secret.Id);
        var update = await service.UpdateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            secret.Id,
            new UpdateSecretRequest("API Token", "api-token", null, "ApiToken"));
        var rotate = await service.RotateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            secret.Id,
            new RotateSecretRequest("new-value"));
        var archive = await service.ArchiveAsync(fixture.CurrentUser, fixture.Customer.Id, secret.Id);

        Assert.Equal(CustomerServiceError.NotFound, get.Error);
        Assert.Equal(CustomerServiceError.NotFound, update.Error);
        Assert.Equal(CustomerServiceError.NotFound, rotate.Error);
        Assert.Equal(CustomerServiceError.NotFound, archive.Error);
    }

    [Fact]
    public async Task SecretService_audit_metadata_does_not_contain_secret_value()
    {
        var fixture = TestFixture.Create(CustomerRole.Owner);

        await fixture.CreateSecretService().CreateAsync(
            fixture.CurrentUser,
            fixture.Customer.Id,
            ValidCreateRequest("api-token", "unique-secret-value"));

        var metadata = fixture.Db.AuditLogEntries.Single().MetadataJson;
        Assert.DoesNotContain("unique-secret-value", metadata);
        Assert.DoesNotContain(fixture.Db.Secrets.Single().ProtectedValue, metadata);
    }

    private static CreateSecretRequest ValidCreateRequest(string slug, string value)
    {
        return new CreateSecretRequest("API Token", slug, null, "ApiToken", value);
    }

    private sealed record CustomerUser(Customer Customer, CurrentUserDto CurrentUser);

    private sealed class TestFixture
    {
        private readonly FakeSecretProtector secretProtector = new();

        private TestFixture(NodeControlTestDbContext db, Customer customer, CurrentUserDto currentUser)
        {
            Db = db;
            Customer = customer;
            CurrentUser = currentUser;
            Clock = new TestClock();
        }

        public NodeControlTestDbContext Db { get; }

        public Customer Customer { get; }

        public CurrentUserDto CurrentUser { get; }

        public TestClock Clock { get; }

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

        public SecretService CreateSecretService()
        {
            return new SecretService(
                Db,
                new CustomerAuthorizationService(Db),
                secretProtector,
                Clock,
                new AuditLogWriter(Db, Clock, new EmptyRequestAuditContext()));
        }

        public SecretReferenceValidationService CreateSecretReferenceValidationService()
        {
            return new SecretReferenceValidationService(Db, new SecretReferenceParser());
        }
    }

    private sealed class FakeSecretProtector : ISecretProtector
    {
        private int nextValue;

        public string Protect(string plaintext)
        {
            nextValue++;
            return $"protected-{nextValue}";
        }

        public string Unprotect(string protectedValue)
        {
            return protectedValue;
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
