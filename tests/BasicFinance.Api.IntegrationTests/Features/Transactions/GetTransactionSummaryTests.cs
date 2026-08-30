using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.IntegrationTests.Features.Transactions;

public class GetTransactionSummaryTests : ApiTestFixtureBase
{
    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    public GetTransactionSummaryTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetTransactionSummary_MonthlyPeriod_ReturnsAggregatesForBothPeriods()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, transactionType: TransactionTypeEnum.Credit, date: new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero), amount: 150.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 7, 10, 0, 0, 0, TimeSpan.Zero), amount: 200.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, transactionType: TransactionTypeEnum.Credit, date: new DateTimeOffset(2026, 7, 28, 0, 0, 0, TimeSpan.Zero), amount: 50.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>("/api/transactions/summary?recordedDate=2026-08-05&timePeriod=Monthly", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousEnd);
        Assert.Equal(2, result.CurrentPeriod.TotalCount);
        Assert.Equal(100.00m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(150.00m, result.CurrentPeriod.TotalIncome);
        Assert.Equal(50.00m, result.CurrentPeriod.NetFlow);
        Assert.Equal(2, result.PreviousPeriod.TotalCount);
        Assert.Equal(200.00m, result.PreviousPeriod.TotalSpend);
        Assert.Equal(50.00m, result.PreviousPeriod.TotalIncome);
        Assert.Equal(-150.00m, result.PreviousPeriod.NetFlow);
    }

    [Fact]
    public async Task GetTransactionSummary_NoQueryParameters_UsesCurrentCalendarMonth()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        var utcNow = DateTime.UtcNow.Date;
        await DbContext.SeedAsync(account, CancellationToken);
        var transaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, amount: 120.00m);
        await DbContext.SeedAsync(transaction, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>("/api/transactions/summary", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(utcNow.Year, utcNow.Month, 1), result.CurrentStart);
        Assert.Equal(result.CurrentStart, result.PreviousEnd);
        Assert.Equal(1, result.CurrentPeriod.TotalCount);
        Assert.Equal(120.00m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(0m, result.CurrentPeriod.TotalIncome);
        Assert.Equal(-120.00m, result.CurrentPeriod.NetFlow);
        Assert.Equal(0, result.PreviousPeriod.TotalCount);
        Assert.Equal(0m, result.PreviousPeriod.TotalSpend);
        Assert.Equal(0m, result.PreviousPeriod.TotalIncome);
        Assert.Equal(0m, result.PreviousPeriod.NetFlow);
    }

    [Fact]
    public async Task GetTransactionSummary_NoTransactions_ReturnsZeroedAggregates()
    {
        // Arrange
        var parameters = "recordedDate=2026-08-05&timePeriod=Monthly";

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousEnd);
        Assert.Equal(0, result.CurrentPeriod.TotalCount);
        Assert.Equal(0m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(0m, result.CurrentPeriod.TotalIncome);
        Assert.Equal(0m, result.CurrentPeriod.NetFlow);
        Assert.Equal(0, result.PreviousPeriod.TotalCount);
        Assert.Equal(0m, result.PreviousPeriod.TotalSpend);
        Assert.Equal(0m, result.PreviousPeriod.TotalIncome);
        Assert.Equal(0m, result.PreviousPeriod.NetFlow);
    }

    [Fact]
    public async Task GetTransactionSummary_WeeklyPeriod_ResolvesCalendarWeekFromMonday()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero), amount: 50.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>("/api/transactions/summary?recordedDate=2026-08-05&timePeriod=Weekly", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 3), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 8, 10), result.CurrentEnd);
        Assert.Equal(new DateOnly(2026, 7, 27), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 8, 3), result.PreviousEnd);
        Assert.Equal(1, result.CurrentPeriod.TotalCount);
        Assert.Equal(100.00m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(1, result.PreviousPeriod.TotalCount);
        Assert.Equal(50.00m, result.PreviousPeriod.TotalSpend);
    }

    [Fact]
    public async Task GetTransactionSummary_UnrecognizedTimePeriod_FallsBackToMonthly()
    {
        // Arrange
        const string parameters = "recordedDate=2026-08-05&timePeriod=Annualish";

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentEnd);
    }

    [Fact]
    public async Task GetTransactionSummary_FilterByAccountId_ReturnsFilteredAggregates()
    {
        // Arrange
        var firstAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Account A");
        var otherAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Account B");
        await DbContext.SeedRangeAsync([firstAccount, otherAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, firstAccount.AccountId, description: "Summary Account A debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, otherAccount.AccountId, description: "Summary Account B debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?recordedDate=2026-08-05&accountId={firstAccount.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(1, result.CurrentPeriod.TotalCount);
        Assert.Equal(100.00m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(0m, result.PreviousPeriod.TotalSpend);
    }

    [Fact]
    public async Task GetTransactionSummary_FilterByInstitutionId_ReturnsFilteredAggregates()
    {
        // Arrange
        var wellsFargoAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Wells Fargo Account");
        var chaseAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Chase Account", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wellsFargoAccount, chaseAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, wellsFargoAccount.AccountId, description: "Wells Fargo debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, chaseAccount.AccountId, description: "Chase debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?recordedDate=2026-08-05&institutionId={TestConstants.ChaseInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(1, result.CurrentPeriod.TotalCount);
        Assert.Equal(40.00m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(0m, result.PreviousPeriod.TotalSpend);
    }

    [Fact]
    public async Task GetTransactionSummary_InstitutionWithNoAccounts_ReturnsZeroedAggregates()
    {
        // Arrange
        var parameters = $"recordedDate=2026-08-05&institutionId={TestConstants.NonExistentInstitutionId}";

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(0, result.CurrentPeriod.TotalCount);
        Assert.Equal(0m, result.CurrentPeriod.TotalSpend);
        Assert.Equal(0m, result.CurrentPeriod.TotalIncome);
        Assert.Equal(0m, result.CurrentPeriod.NetFlow);
        Assert.Equal(0, result.PreviousPeriod.TotalCount);
        Assert.Equal(0m, result.PreviousPeriod.TotalSpend);
        Assert.Equal(0m, result.PreviousPeriod.TotalIncome);
        Assert.Equal(0m, result.PreviousPeriod.NetFlow);
    }

    [Fact]
    public async Task GetTransactionSummary_InstitutionFilter_ExcludesTransactionsFromInactiveAccounts()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Active Account");
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Inactive Account");
        inactiveAccount.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, activeAccount.AccountId, description: "Active account debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, inactiveAccount.AccountId, description: "Inactive account debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?recordedDate=2026-08-05&institutionId={TestConstants.WellsFargoInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(1, result.CurrentPeriod.TotalCount);
        Assert.Equal(100.00m, result.CurrentPeriod.TotalSpend);
    }

    [Fact]
    public async Task GetTransactionSummary_InstitutionFilter_ExcludesTransactionsFromInactiveInstitution()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Summary Inactive Institution Account", institutionId: inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Inactive institution debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m);
        await DbContext.SeedAsync(transaction, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>($"/api/transactions/summary?recordedDate=2026-08-05&institutionId={inactiveInstitution.InstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(0, result.CurrentPeriod.TotalCount);
        Assert.Equal(0m, result.CurrentPeriod.TotalSpend);
    }

    [Fact]
    public async Task GetTransactionSummary_TransactionsFromOtherUsers_AreExcluded()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        var otherAccount = AccountFactory.Create(OtherUserId);
        await DbContext.SeedRangeAsync([account, otherAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Own debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(OtherUserId, otherAccount.AccountId, description: "Other user debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 999.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<TransactionSummaryResponseDto>("/api/transactions/summary?recordedDate=2026-08-05", CancellationToken);

        // Assert
        Assert.Equal(1, result.CurrentPeriod.TotalCount);
        Assert.Equal(100.00m, result.CurrentPeriod.TotalSpend);
    }
}
