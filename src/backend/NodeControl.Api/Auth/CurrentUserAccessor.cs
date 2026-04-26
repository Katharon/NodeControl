using System.Security.Claims;
using NodeControl.Application.Abstractions.Auth;

namespace NodeControl.Api.Auth;

public sealed class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    Microsoft.Extensions.Options.IOptions<AuthOptions> authOptions)
    : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public string? AuthProvider => FindClaim(FakeAuthDefaults.AuthProviderClaim)
        ?? (authOptions.Value.UseOidc ? authOptions.Value.Oidc.Provider : null);

    public string? ExternalSubject => FindClaim(ClaimTypes.NameIdentifier)
        ?? FindClaim("sub");

    public string? Email => FindClaim(ClaimTypes.Email)
        ?? FindClaim("email");

    public string? DisplayName => FindClaim(ClaimTypes.Name)
        ?? FindClaim("name")
        ?? Email;

    public bool IsPlatformAdmin => bool.TryParse(
        FindClaim(FakeAuthDefaults.IsPlatformAdminClaim),
        out var isPlatformAdmin) && isPlatformAdmin;

    private string? FindClaim(string claimType)
    {
        return User?.FindFirstValue(claimType);
    }
}
