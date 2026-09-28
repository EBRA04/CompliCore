using System.Net;
using System.Net.Http.Json;
using CompliCore.DTOs.ComplianceItemDtos;
using CompliCore.DTOs.EmployeeDtos;
using CompliCore.Enums;
using Xunit;

namespace CompliCore.Tests;

public class ComplianceItemIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ComplianceItemIsolationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TenantB_CannotAccess_TenantA_ComplianceItem()
    {
        var (clientA, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var (clientB, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Buraydah Trading Co", $"b-{Guid.NewGuid()}@test.example");

        var empResponse = await clientA.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Mohammed Khan", "Pakistani", "2412345678", "Foreman"));
        var employee = await empResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        var itemResponse = await clientA.PostAsJsonAsync("/api/compliance-items", new CreateComplianceItemRequest(
            employee!.Id, ComplianceItemType.Iqama, null, "2412345678",
            null, new DateOnly(2026, 11, 15), null));
        var item = await itemResponse.Content.ReadFromJsonAsync<ComplianceItemResponse>();

        var getAsB = await clientB.GetAsync($"/api/compliance-items/{item!.Id}");
        var deleteAsB = await clientB.DeleteAsync($"/api/compliance-items/{item.Id}");
        var getAsA = await clientA.GetAsync($"/api/compliance-items/{item.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getAsB.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteAsB.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getAsA.StatusCode);
    }
}