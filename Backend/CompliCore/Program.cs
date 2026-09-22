using CompliCore.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Registers the health-check SYSTEM with DI, and tells it to actually try
// a real query against AppDbContext (not just "is the process alive").
// This must be on builder.Services, BEFORE builder.Build().
// Needs the Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore package.
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// REMOVED: app.UseHttpsRedirection();
// No cert exists inside the Docker container — it only ever serves plain
// HTTP on 8080 there. This line would try to redirect every request to a
// nonexistent HTTPS port once running in Docker.

app.UseAuthorization();

app.MapControllers();

// Turns the health-check registration above into an actual reachable route.
// Without this line, GET /health still 404s — registering the check and
// mapping it to a URL are two separate steps.
// docker-compose's healthcheck will call: curl -f http://localhost:8080/health
app.MapHealthChecks("/health");

// Runs pending migrations automatically on every startup — this is what
// makes "clean clone -> docker compose up --build -> working schema" true,
// instead of requiring someone to remember `dotnet ef database update` by hand.
//
// Why the scope: AppDbContext is registered as SCOPED (one instance per
// HTTP request). Outside of a real request there's no request to scope it
// to, so we manually open a scope, resolve AppDbContext from THAT scope,
// use it, and `using` disposes the scope (and the context) when done.
// Calling GetRequiredService<AppDbContext>() directly on app.Services with
// no scope would throw — you can't resolve a Scoped service from the app's
// root container.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.Run();

// Top-level statement files like this get compiled into an implicit class
// also named Program — but it's `internal` by default, so another project
// (the test project) can't see it. This line reopens that same generated
// class as `public partial`, purely so WebApplicationFactory<Program> in
// the integration tests (Day 10+) can find it. Does nothing today.
public partial class Program { }