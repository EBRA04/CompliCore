using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace CompliCore.Tests;
//this file is used to create a test factory for the API, which will be used to create a test server and client for integration tests.
//It uses Testcontainers to spin up a PostgreSQL container for testing purposes.
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg =
    new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _pg.StartAsync();

    Task IAsyncLifetime.DisposeAsync() => _pg.DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Default", _pg.GetConnectionString());
        builder.UseSetting("Jwt:Key", "test-key-test-key-test-key-test-key-1234");
        builder.UseSetting("Jwt:Issuer", "complicore");
        builder.UseSetting("Jwt:Audience", "complicore-clients");
        builder.UseSetting("Jwt:AccessTokenExpiryMinutes", "480");
    }
}