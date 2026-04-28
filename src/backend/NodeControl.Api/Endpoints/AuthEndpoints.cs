using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using NodeControl.Api.Auth;

namespace NodeControl.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/login", (IOptions<AuthOptions> authOptions) =>
        {
            if (authOptions.Value.UseFakeAuth)
            {
                return Results.Redirect("/dashboard");
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/dashboard" },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        });

        endpoints.MapPost("/auth/logout", (IOptions<AuthOptions> authOptions) =>
            SignOut(authOptions.Value));

        endpoints.MapGet("/auth/logout", (IOptions<AuthOptions> authOptions) =>
            SignOut(authOptions.Value));

        return endpoints;
    }

    private static IResult SignOut(AuthOptions authOptions)
    {
        if (authOptions.UseFakeAuth)
        {
            return Results.Redirect("/");
        }

        return Results.SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            [CookieAuthenticationDefaults.AuthenticationScheme]);
    }
}
