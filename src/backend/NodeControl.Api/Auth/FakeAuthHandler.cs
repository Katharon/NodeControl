using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace NodeControl.Api.Auth;

public static class FakeAuthDefaults
{
    public const string AuthenticationScheme = "Fake";

    public const string AuthProviderClaim = "nodecontrol:auth_provider";

    public const string IsPlatformAdminClaim = "nodecontrol:is_platform_admin";
}

public sealed class FakeAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<AuthOptions> authOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var fake = authOptions.Value.Fake;
        var claims = new List<Claim>
        {
            new(FakeAuthDefaults.AuthProviderClaim, fake.Provider),
            new(ClaimTypes.NameIdentifier, fake.Subject),
            new("sub", fake.Subject),
            new(ClaimTypes.Email, fake.Email),
            new("email", fake.Email),
            new(ClaimTypes.Name, fake.DisplayName),
            new("name", fake.DisplayName),
            new(FakeAuthDefaults.IsPlatformAdminClaim, fake.IsPlatformAdmin.ToString())
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
