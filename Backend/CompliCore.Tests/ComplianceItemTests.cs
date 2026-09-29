using CompliCore.DTOs;
using CompliCore.DTOs.AuditLogDtos;
using CompliCore.DTOs.ComplianceItemDtos;
using CompliCore.DTOs.EmployeeDtos;
using CompliCore.Enums;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CompliCore.Tests;

public class ComplianceItemTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ComplianceItemTests(ApiFactory factory)
    {
        _factory = factory;
    }
    [Fact]
    public async Task CreateIqama_ForEmployeeWithoutIqamaNumber_ReturnsBadRequest()
    {
        var (client, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var empResponse = await client.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("No Iqama", "Saudi", null, null));
        var employee = await empResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        var response = await client.PostAsJsonAsync("/api/compliance-items",
            new CreateComplianceItemRequest(employee!.Id, ComplianceItemType.Iqama, null, null, null,
                new DateOnly(2026, 11, 15), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateIqama_ForEmployee_ReturnsCreated_WithComputedStatus()
    {
        var (client, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var empResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Mohammed Khan", "Pakistani", "2412345678", "Foreman"));
        var employee = await empResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        var itemResponse = await client.PostAsJsonAsync("/api/compliance-items", new CreateComplianceItemRequest(
            employee!.Id, ComplianceItemType.Iqama, null, "2412345678",
            new DateOnly(2025, 3, 1), new DateOnly(2026, 11, 15), "Renewal requested"));

        Assert.Equal(HttpStatusCode.Created, itemResponse.StatusCode);

        var item = await itemResponse.Content.ReadFromJsonAsync<ComplianceItemResponse>();
        Assert.Equal("Iqama", item!.Title); // auto-filled since Title was null
        Assert.True(item.DaysRemaining > 0);
    }

    [Fact]
    public async Task CreateIqama_WithNoEmployeeId_ReturnsBadRequest()
    {
        var (client, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var response = await client.PostAsJsonAsync("/api/compliance-items", new CreateComplianceItemRequest(
            null, ComplianceItemType.Iqama, null, null, null, new DateOnly(2026, 11, 15), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCommercialRegistration_WithEmployeeId_ReturnsBadRequest()
    {
        var (client, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var empResponse = await client.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Mohammed Khan", "Pakistani", "2412345678", "Foreman"));
        var employee = await empResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        var response = await client.PostAsJsonAsync("/api/compliance-items", new CreateComplianceItemRequest(
            employee!.Id, ComplianceItemType.CommercialRegistration, null, null, null, new DateOnly(2027, 1, 1), null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletingEmployee_AuditsTheirDeletedItems()
    {
        var (client, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var empResponse = await client.PostAsJsonAsync("/api/employees",
            new CreateEmployeeRequest("Mohammed Khan", "Pakistani", "2412345678", "Foreman"));
        var employee = await empResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        await client.PostAsJsonAsync("/api/compliance-items", new CreateComplianceItemRequest(
            employee!.Id, ComplianceItemType.Iqama, null, null, null, new DateOnly(2026, 11, 15), null));

        var delete = await client.DeleteAsync($"/api/employees/{employee.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var audit = await client.GetFromJsonAsync<PagedResult<AuditLogResponse>>(
            "/api/audit-logs?entityName=ComplianceItem");
        Assert.Contains(audit!.Items, a => a.Action == "Deleted");
    }
}