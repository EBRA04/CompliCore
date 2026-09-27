using System.Net;
using System.Net.Http.Json;
using CompliCore.DTOs.AuthDtos;
using Xunit;

namespace CompliCore.Tests;

public class TenantIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public TenantIsolationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TenantB_CannotSee_TenantA_Profile()
    {
        var (clientA, authA) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"a-{Guid.NewGuid()}@test.example");

        var (clientB, authB) = await TestHelpers.RegisterTenantAsync(
            _factory, "Buraydah Trading Co", $"b-{Guid.NewGuid()}@test.example");

        var meAResponse = await clientA.GetAsync("/api/auth/me");
        var meA = await meAResponse.Content.ReadFromJsonAsync<UserSummary>();

        var meBResponse = await clientB.GetAsync("/api/auth/me");
        var meB = await meBResponse.Content.ReadFromJsonAsync<UserSummary>();

        Assert.Equal(HttpStatusCode.OK, meAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meBResponse.StatusCode);

        Assert.Equal(authA.User.TenantId, meA!.TenantId);
        Assert.Equal(authB.User.TenantId, meB!.TenantId);
        Assert.NotEqual(meA.TenantId, meB.TenantId);
    }
}