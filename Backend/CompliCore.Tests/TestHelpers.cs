using System.Net.Http.Json;
using CompliCore.DTOs.AuthDtos;

namespace CompliCore.Tests;

public static class TestHelpers
{
    public static async Task<(HttpClient Client, AuthResponse Auth)> RegisterTenantAsync(
        ApiFactory factory, string companyName, string email)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            CompanyName = companyName,
            FullName = "Test Admin",
            Email = email,
            Password = "Str0ngPass!"
        });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);

        return (client, auth);
    }
}