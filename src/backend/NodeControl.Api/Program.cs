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
app.MapCustomersEndpoints();
app.MapCustomerMembershipsEndpoints();

app.Run();

public partial class Program;
