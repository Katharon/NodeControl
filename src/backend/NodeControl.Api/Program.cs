using NodeControl.Api;
using NodeControl.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNodeControlApi(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapAuthEndpoints();
app.MapMeEndpoints();
app.MapUsersEndpoints();
app.MapCustomersEndpoints();
app.MapCustomerMembershipsEndpoints();
app.MapCustomerUserLookupEndpoints();
app.MapHostConnectionChecksEndpoints();
app.MapControlNodesEndpoints();
app.MapManagedNodesEndpoints();
app.MapInventoryGroupsEndpoints();
app.MapPlaybooksEndpoints();
app.MapVariableSetsEndpoints();
app.MapTemplatesEndpoints();
app.MapSecretsEndpoints();
app.MapSecretReferencesEndpoints();
app.MapJobsEndpoints();
app.MapJobRunsEndpoints();
app.MapSchedulesEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program;
