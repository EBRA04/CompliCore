using System.Net;
using System.Net.Http.Json;
using CompliCore.DTOs.AuthDtos;
using CompliCore.DTOs.EmployeeDtos;
using CompliCore.DTOs.UserDtos;
using CompliCore.Enums;
using Xunit;

namespace CompliCore.Tests;

public class AuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Viewer_CanRead_ButNotWrite()
    {
        var (adminClient, _) = await TestHelpers.RegisterTenantAsync(
            _factory, "Al-Noor Contracting", $"admin-{Guid.NewGuid()}@test.example");

        var viewerEmail = $"viewer-{Guid.NewGuid()}@test.example";
        var createUserResponse = await adminClient.PostAsJsonAsync("/api/users", new CreateUserRequest(
            "Viewer User", viewerEmail, "Str0ngPass!", UserRole.Viewer));
        createUserResponse.EnsureSuccessStatusCode();

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = viewerEmail,
            Password = "Str0ngPass!"
        });
        var viewerAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var viewerClient = _factory.CreateClient();
        viewerClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", viewerAuth!.Token);

        var getResponse = await viewerClient.GetAsync("/api/employees");
        var createResponse = await viewerClient.PostAsJsonAsync("/api/employees", new CreateEmployeeRequest(
            "Should Fail", "Test", null, null));

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }
}