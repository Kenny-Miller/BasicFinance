using BasicFinance.Api.IntegrationTests.Constants;
using System.Net.Http.Json;
using System.Text.Json;

namespace BasicFinance.Api.IntegrationTests.Helpers;

public static class KeycloakHelper
{
    public static async Task ProvisionTestRealmAsync(string keycloakBaseAddress)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        using var httpClient = new HttpClient(handler);

        var adminToken = await GetAdminAccessTokenAsync(httpClient, keycloakBaseAddress);

        httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

        await CreateRealmAsync(httpClient, keycloakBaseAddress);
        await CreateTestUserAsync(httpClient, keycloakBaseAddress);
        await CreateTestClientAsync(httpClient, keycloakBaseAddress);
    }

    private static async Task<string> GetAdminAccessTokenAsync(HttpClient httpClient, string keycloakBaseAddress)
    {
        var response = await httpClient.PostAsync(
            $"{keycloakBaseAddress}/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("username", "admin"),
                new KeyValuePair<string, string>("password", "admin"),
                new KeyValuePair<string, string>("client_id", "admin-cli"),
            }));

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("access_token").ToString();
    }

    private static async Task CreateRealmAsync(HttpClient httpClient, string keycloakBaseAddress)
    {
        var realm = new
        {
            Realm = "basic-hub",
            Enabled = true
        };

        var response = await httpClient.PostAsJsonAsync(
            $"{keycloakBaseAddress}/admin/realms",
            realm);

        if (response.StatusCode is System.Net.HttpStatusCode.Conflict)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task CreateTestUserAsync(HttpClient httpClient, string keycloakBaseAddress)
    {
        var user = new
        {
            Username = TestConstants.TestUsername,
            Email = "test@basicfinance.local",
            Enabled = true,
            EmailVerified = true,
            UsernameLiteral = TestConstants.TestUsername,
            Credentials = new[]
            {
                new
                {
                    Type = "password",
                    Value = TestConstants.TestPassword,
                    Temporary = false
                }
            },
            Attributes = new
            {
                firstName = "Test",
                lastName = "User"
            }
        };

        var response = await httpClient.PostAsJsonAsync(
            $"{keycloakBaseAddress}/admin/realms/basic-hub/users",
            user);

        if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private static async Task CreateTestClientAsync(HttpClient httpClient, string keycloakBaseAddress)
    {
        var client = new
        {
            ClientId = TestConstants.TestClientId,
            Enabled = true,
            ClientAuthenticatorType = "client-secret",
            Secret = TestConstants.TestClientSecret,
            DirectAccessGrantsEnabled = true,
            PublicClient = false,
            FullScopeAllowed = true
        };

        var response = await httpClient.PostAsJsonAsync(
            $"{keycloakBaseAddress}/admin/realms/basic-hub/clients",
            client);

        if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.BadRequest)
        {
            return;
        }

        response.EnsureSuccessStatusCode();
    }
}
