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
    string InstitutionCode,
    decimal? LatestBalance,
    DateTimeOffset? LatestBalanceRecordedDate);

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
    decimal LatestBalance,
    DateTimeOffset LatestBalanceRecordedDate);

public record InstitutionSummaryResponseDto(
    int InstitutionId,
    string InstitutionName,
    IEnumerable<InstitutionSummaryAccountDto> Accounts,
    Dictionary<string, decimal> AccountTypeTotals,
    Dictionary<string, decimal> AccountTypePreviousTotals,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    DateTime PreviousPeriodStart,
    DateTime PreviousPeriodEnd);
