namespace BasicFinance.Api.IntegrationTests.Helpers;

public record InstitutionDto(
    int Id,
    string Code,
    string Name,
    string? LogoUrl);

public record AccountDto(
    Guid Id,
    string Name,
    string AccountTypeCode,
    string Institution,
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

public record BalanceSummaryAccountDto(
    Guid Id,
    string AccountTypeCode,
    string Institution,
    string AccountName,
    decimal Balance,
    decimal PercentageOfTotalBalance,
    decimal PercentageOfAccountTypeBalance);

public record BalanceSummaryAccountTypeDto(
    decimal Balance,
    decimal PercentageOfTotalBalance,
    List<BalanceSummaryAccountDto> Accounts);

public record BalanceSummaryPeriodDto(
    decimal Balance,
    Dictionary<string, BalanceSummaryAccountTypeDto> AccountTypeBreakdowns);

public record BalanceSummaryResponseDto(
    BalanceSummaryPeriodDto CurrentPeriodBreakdown,
    BalanceSummaryPeriodDto PreviousPeriodBreakdown,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    DateOnly PreviousPeriodStart,
    DateOnly PreviousPeriodEnd);

public record InstitutionSummaryAccountDto(
    Guid Id,
    string Name,
    string AccountTypeCode,
    decimal Balance,
    DateTimeOffset BalanceRecordedDate);

public record InstitutionSummaryResponseDto(
    int InstitutionId,
    string InstitutionName,
    IEnumerable<InstitutionSummaryAccountDto> Accounts,
    Dictionary<string, decimal> AccountTypeTotals,
    Dictionary<string, decimal> AccountTypePreviousTotals,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    DateOnly PreviousPeriodStart,
    DateOnly PreviousPeriodEnd);
