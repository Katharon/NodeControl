namespace NodeControl.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Mode { get; set; } = "Oidc";

    public FakeAuthOptions Fake { get; set; } = new();

    public OidcOptions Oidc { get; set; } = new();

    public bool UseFakeAuth => Mode.Equals("Fake", StringComparison.OrdinalIgnoreCase);

    public bool UseOidc => Mode.Equals("Oidc", StringComparison.OrdinalIgnoreCase);
}

public sealed class FakeAuthOptions
{
    public string Provider { get; set; } = "fake";

    public string Subject { get; set; } = "dev-admin";

    public string Email { get; set; } = "dev-admin@nodecontrol.local";

    public string DisplayName { get; set; } = "Dev Admin";

    public bool IsPlatformAdmin { get; set; } = true;
}

public sealed class OidcOptions
{
    public string Provider { get; set; } = "oidc";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string CallbackPath { get; set; } = "/signin-oidc";

    public bool RequireHttpsMetadata { get; set; } = true;
}
