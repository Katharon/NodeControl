using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using NodeControl.Api.Audit;
using NodeControl.Api.Auth;
using NodeControl.Application.Audit;
using NodeControl.Application.Abstractions.Authorization;
using NodeControl.Application.Abstractions.Auth;
using NodeControl.Application.Authorization;
using NodeControl.Application.Auth;
using NodeControl.Application.ControlNodes;
using NodeControl.Application.Customers;
using NodeControl.Application.GitRepositories;
using NodeControl.Application.HostConnectionChecks;
using NodeControl.Application.InventoryGroups;
using NodeControl.Application.JobRuns;
using NodeControl.Application.Jobs;
using NodeControl.Application.ManagedNodes;
using NodeControl.Application.Memberships;
using NodeControl.Application.Playbooks;
using NodeControl.Application.Schedules;
using NodeControl.Application.Secrets;
using NodeControl.Application.Templates;
using NodeControl.Application.Users;
using NodeControl.Application.Validation;
using NodeControl.Application.VariableSets;
using NodeControl.Infrastructure;

namespace NodeControl.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddNodeControlApi(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
            ?? new AuthOptions();

        ValidateAuthOptions(authOptions, environment);

        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserAccessor>();
        services.AddScoped<IRequestAuditContext, HttpRequestAuditContext>();
        services.AddScoped<UserProvisioningService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ICustomerAuthorizationService, CustomerAuthorizationService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<HostConnectionCheckService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<CustomerMembershipService>();
        services.AddScoped<UserService>();
        services.AddScoped<UserLookupService>();
        services.AddScoped<ControlNodeService>();
        services.AddScoped<ManagedNodeService>();
        services.AddScoped<InventoryGroupService>();
        services.AddScoped<InventoryPreviewService>();
        services.AddScoped<YamlJsonValidationService>();
        services.AddScoped<PlaybookService>();
        services.AddScoped<GitRepositoryService>();
        services.AddScoped<VariableSetService>();
        services.AddScoped<TemplateValidationService>();
        services.AddSingleton<SecretReferenceParser>();
        services.AddScoped<SecretReferenceValidationService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<SecretService>();
        services.AddScoped<JobService>();
        services.AddScoped<JobRunService>();
        services.AddScoped<JobRunLogService>();
        services.AddSingleton<ICronScheduleCalculator, CronScheduleCalculator>();
        services.AddScoped<JobScheduleService>();

        services.AddNodeControlInfrastructure(configuration);

        var authenticationBuilder = services.AddAuthentication(options =>
        {
            if (authOptions.UseFakeAuth)
            {
                options.DefaultAuthenticateScheme = FakeAuthDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = FakeAuthDefaults.AuthenticationScheme;
            }
            else
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            }
        });

        authenticationBuilder.AddCookie(options =>
        {
            options.Cookie.Name = "nodecontrol.session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.LoginPath = "/auth/login";
            options.LogoutPath = "/auth/logout";
            options.AccessDeniedPath = "/login";
        });

        authenticationBuilder.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, FakeAuthHandler>(
            FakeAuthDefaults.AuthenticationScheme,
            _ => { });

        authenticationBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.Authority = authOptions.Oidc.Authority;
            options.ClientId = authOptions.Oidc.ClientId;
            options.ClientSecret = authOptions.Oidc.ClientSecret;
            options.CallbackPath = authOptions.Oidc.CallbackPath;
            options.ResponseType = "code";
            options.SaveTokens = false;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.RequireHttpsMetadata = authOptions.Oidc.RequireHttpsMetadata;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
        });

        services.AddAuthorization();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
        });
        services.AddOpenApi();

        return services;
    }

    private static void ValidateAuthOptions(AuthOptions authOptions, IWebHostEnvironment environment)
    {
        if (environment.IsProduction() && authOptions.UseFakeAuth)
        {
            throw new InvalidOperationException("Fake Auth cannot be enabled in Production.");
        }

        if (!authOptions.UseFakeAuth && !authOptions.UseOidc)
        {
            throw new InvalidOperationException("Auth:Mode must be either Fake or Oidc.");
        }

        if (environment.IsProduction() && authOptions.UseOidc)
        {
            if (string.IsNullOrWhiteSpace(authOptions.Oidc.Authority)
                || string.IsNullOrWhiteSpace(authOptions.Oidc.ClientId))
            {
                throw new InvalidOperationException("Production OIDC requires Auth:Oidc:Authority and Auth:Oidc:ClientId.");
            }
        }
    }
}
