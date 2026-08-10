using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

namespace BasicFinance.Api.IntegrationTests.Helpers;

public record InstitutionDto(
    int Id,
    string InstitutionCode,
    string Name,
    string? LogoUrl);

public record MyInstitutionDto(int InstitutionId, string Name);

public record AccountDto(
    Guid Id,
    string AccountTypeCode,
    string Institution,
    string AccountName,
    decimal Balance,
    DateTimeOffset BalanceRecordedDate);

public record TransactionDto(
    Guid Id,
    string TransactionTypeName,
    string TransactionCategoryName,
    string AccountName,
    DateTimeOffset Date,
    decimal Amount,
    string Description);

public record KeycloakCredentialDto(string Username, string Password);

public record KeycloakUserDto(
    string UserId,
    string Username,
    string AccessToken);

public record KeycloakTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("refresh_expires_in")] int RefreshExpiresIn,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("token_type")] string TokenType,
    [property: JsonPropertyName("not-before-policy")] int NotBeforePolicy,
    [property: JsonPropertyName("session_state")] string SessionState,
    [property: JsonPropertyName("scope")] string Scope
);

public record KeycloakUserCacheEntry(KeycloakTokenResponse KeycloakTokenResponse, JwtSecurityToken JwtSecurityToken);