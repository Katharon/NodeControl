using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NodeControl.Infrastructure.Persistence;

public sealed class DesignTimeNodeControlDbContextFactory
    : IDesignTimeDbContextFactory<NodeControlDbContext>
{
    public NodeControlDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NODECONTROL_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=nodecontrol;Username=nodecontrol;Password=nodecontrol_dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<NodeControlDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new NodeControlDbContext(optionsBuilder.Options);
    }
}
