using BasicFinance.Domain.Enums;

namespace BasicFinance.Api.IntegrationTests.Helpers;

public record InstitutionDto(
    int Id,
    string Code,
    string Name,
    string? LogoUrl);

public record AccountTypeDto(
    int Id,
    string Code,
    string Name,
    bool IsLiability);

public record TransactionTypeDto(
    int Id,
    string Code,
    string Name);

public record TransactionCategoryDto(
    int Id,
    string Code,
    string Name);

public record AccountDto(
    Guid Id,
    string Name,
    string AccountTypeCode,
    string AccountTypeName,
    string InstitutionCode,
    string InstitutionName,
    string Currency,
    bool IsLiability,
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
    decimal PercentageOfAccountTypeBalance,
    string Currency,
    DateTimeOffset BalanceRecordedDate,
    decimal? PreviousBalance,
    decimal? Change);

public record BalanceSummaryAccountTypeDto(
    decimal Balance,
    decimal PercentageOfTotalBalance,
    List<BalanceSummaryAccountDto> Accounts,
    string AccountTypeName,
    bool IsLiability);

public record BalanceSummaryPeriodDto(
    decimal Balance,
    Dictionary<string, BalanceSummaryAccountTypeDto> AccountTypeBreakdowns);

public record BalanceSummaryResponseDto(
    BalanceSummaryPeriodDto CurrentPeriodBreakdown,
    BalanceSummaryPeriodDto PreviousPeriodBreakdown,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset PreviousPeriodStart,
    DateTimeOffset PreviousPeriodEnd);

public record InstitutionSummaryAccountDto(
    Guid Id,
    string Name,
    string AccountTypeCode,
    string AccountTypeName,
    string InstitutionCode,
    string InstitutionName,
    string Currency,
    bool IsLiability,
    decimal LatestBalance,
    DateTimeOffset LatestBalanceRecordedDate);

public record InstitutionSummaryResponseDto(
    int InstitutionId,
    string InstitutionName,
    IEnumerable<InstitutionSummaryAccountDto> Accounts,
    Dictionary<string, decimal> AccountTypeTotals,
    Dictionary<string, decimal> AccountTypePreviousTotals,
    DateTimeOffset CurrentPeriodStart,
    DateTimeOffset CurrentPeriodEnd,
    DateTimeOffset PreviousPeriodStart,
    DateTimeOffset PreviousPeriodEnd);

public record BalanceHistoryPointDto(
    DateTimeOffset BalanceDate,
    decimal Balance);

public record BalanceHistoryResponseDto(
    Guid AccountId,
    string AccountName,
    string AccountTypeCode,
    string AccountTypeName,
    string Institution,
    string Currency,
    List<BalanceHistoryPointDto> Points);

public record NetWorthPointDto(
    DateTimeOffset PeriodStart,
    decimal NetWorth);

public record NetWorthOverTimeResponseDto(
    TimePeriod TimePeriod,
    List<NetWorthPointDto> Points);

public record TransactionPeriodSummaryDto(
    int TotalCount,
    decimal TotalSpend,
    decimal TotalIncome,
    decimal NetFlow);

public record TransactionSummaryResponseDto(
    DateOnly CurrentStart,
    DateOnly CurrentEnd,
    DateOnly PreviousStart,
    DateOnly PreviousEnd,
    TransactionPeriodSummaryDto CurrentPeriod,
    TransactionPeriodSummaryDto PreviousPeriod);

public record DailyTransactionSummaryDto(
    DateOnly Date,
    decimal TotalSpend,
    int TransactionCount);

public record DailySummaryResponseDto(
    DateOnly CurrentStart,
    DateOnly CurrentEnd,
    DateOnly PreviousStart,
    DateOnly PreviousEnd,
    List<DailyTransactionSummaryDto> CurrentPeriod,
    List<DailyTransactionSummaryDto> PreviousPeriod);

public record AccountBalanceSummaryAccountDto(
    Guid Id,
    string AccountTypeCode,
    string Institution,
    string AccountName,
    decimal Balance,
    decimal PercentageOfTotalBalance,
    decimal PercentageOfAccountTypeBalance);

public record AccountBalanceSummaryAccountTypeDto(
    decimal Balance,
    decimal PercentageOfTotalBalance,
    List<AccountBalanceSummaryAccountDto> Accounts);

public record AccountBalanceSummaryPeriodDto(
    decimal Balance,
    Dictionary<string, AccountBalanceSummaryAccountTypeDto> AccountTypeBreakdowns);

public record AccountBalanceSummaryResponseDto(
    AccountBalanceSummaryPeriodDto CurrentPeriodBreakdown,
    AccountBalanceSummaryPeriodDto PreviousPeriodBreakdown,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    DateOnly PreviousPeriodStart,
    DateOnly PreviousPeriodEnd);
