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
