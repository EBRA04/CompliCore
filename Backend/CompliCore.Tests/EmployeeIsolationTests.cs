using System.Net;
using System.Net.Http.Json;
using CompliCore.DTOs.EmployeeDtos;
using Xunit;

namespace CompliCore.Tests;

public class EmployeeIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public EmployeeIsolationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TenantB_CannotAccess_TenantA_Employee()
    {
        // Arrange: two separate tenants, each with their own authorized client
        var (clientA, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var (clientB, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Buraydah Trading Co", $"b-{Guid.NewGuid()}@test.example");

        // Act 1: tenant A creates an employee
        var createResponse = await clientA.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Mohammed Khan", "Pakistani", "2412345678", "Foreman"));

        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Act 2: tenant B tries to fetch that same employee by id
        var getAsB = await clientB.GetAsync($"/api/employees/{created!.Id}");

        // Act 3: tenant A fetches their own employee, to confirm it still works for the owner
        var getAsA = await clientA.GetAsync($"/api/employees/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, getAsB.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getAsA.StatusCode);
    }
    [Fact]
    public async Task TwoEmployees_WithoutIqama_BothSucceed()
    {
        var (client, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var first = await client.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Ali Hassan", "Egyptian", "", null));
        var second = await client.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Omar Saleh", "Sudanese", "", null));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}