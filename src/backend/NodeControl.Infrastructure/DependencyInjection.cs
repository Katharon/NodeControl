using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodeControl.Application.Abstractions.Persistence;
using NodeControl.Application.Abstractions.Time;
using NodeControl.Infrastructure.Persistence;
using NodeControl.Infrastructure.Time;

namespace NodeControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNodeControlInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("NodeControl")
            ?? "Host=localhost;Port=5432;Database=nodecontrol;Username=nodecontrol;Password=nodecontrol_dev_password";

        services.AddDbContext<NodeControlDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<INodeControlDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<NodeControlDbContext>());

        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
