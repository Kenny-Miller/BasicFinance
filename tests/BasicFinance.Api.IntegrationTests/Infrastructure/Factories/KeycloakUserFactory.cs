using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BasicFinance.Api.IntegrationTests.Infrastructure.Factories
{
    public sealed class KeycloakUserFactory : IDisposable
    {
        private const string _testRealm = "basic-hub";
        private const string _testClientId = "basic-finance-public";
        private static readonly Dictionary<TestUser, KeycloakCredentialDto> _testUserCredentials = new()
        {
            [TestUser.One] = new("testuser1", "password1"),
            [TestUser.Two] = new("testuser2", "password2"),
            [TestUser.Three] = new("testuser3", "password3")
        };

        private static readonly MemoryCache _userCache = new(Options.Create(new MemoryCacheOptions()));
        private static readonly HttpClient _httpClient = new();
        private readonly string _keycloakBaseUrl;

        public KeycloakUserFactory(string keycloakBaseUrl)
        {
            _keycloakBaseUrl = keycloakBaseUrl;
        }

        /// <summary>
        /// Gets the specified user's authentication token and user info from Keycloak.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">If unable to retrieve the user.</exception>
        public async Task<KeycloakUserCacheEntry> GetUserAsync(TestUser user)
        {
            var result = await _userCache.GetOrCreateAsync(user, (x) => GetUserAsync(x, user));
            return result ?? throw new InvalidOperationException($"Cache record for user {user} was unable to be retrieved/created");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _userCache.Dispose();
            _httpClient.Dispose();
        }

        /// <summary>
        /// Gets the specified user's authentication token and user info from Keycloak.
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="testUser"></param>
        /// <returns></returns>
        private async Task<KeycloakUserCacheEntry> GetUserAsync(ICacheEntry entry, TestUser testUser)
        {
            var tokenResponse = await GetTokenAsync(testUser);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenResponse.AccessToken);

            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(tokenResponse.ExpiresIn);
            return new(tokenResponse, token);
        }

        /// <summary>
        /// Retrieves the token of specified user from Keycloak token endpoint.
        /// </summary>
        /// <param name="testUser"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<KeycloakTokenResponse> GetTokenAsync(TestUser testUser)
        {
            var testUserCredentials = _testUserCredentials[testUser];
            var tokenParams = new List<KeyValuePair<string, string>>([
                new("grant_type", "password"),
                new("username", testUserCredentials.Username),
                new("password", testUserCredentials.Password),
                new("client_id", _testClientId),
            ]);

            var requestUrl = $"{_keycloakBaseUrl}/realms/${_testRealm}/protocol/openid-connect/token";
            var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(tokenParams));

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Token request failed with {response.StatusCode}. Body: {errorBody}");
            }

            var json = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
            if (json == null)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Token deserialization failed with {response.StatusCode}. Body: {errorBody}");
            }

            return json;
        }
    }
}
