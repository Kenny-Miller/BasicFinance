using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using Xunit;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.IntegrationTests.Features.Transactions;

public class GetDailyTransactionSummaryTests : ApiTestFixtureBase
{
    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    public GetDailyTransactionSummaryTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    [Fact]
    public async Task GetDailyTransactionSummary_MonthlyPeriod_ReturnsZeroFilledDailyPoints()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Morning debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Afternoon debit", date: new DateTimeOffset(2026, 8, 5, 14, 30, 0, TimeSpan.Zero), amount: 40.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Credit", transactionType: TransactionTypeEnum.Credit, date: new DateTimeOffset(2026, 8, 6, 0, 0, 0, TimeSpan.Zero), amount: 150.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>("/api/transactions/dailySummary?recordedDate=2026-08-05&timePeriod=Monthly", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 9, 1), result.CurrentEnd);
        Assert.Equal(new DateOnly(2026, 7, 1), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 8, 1), result.PreviousEnd);
        Assert.Equal(31, result.CurrentPeriod.Count);
        Assert.Equal(31, result.PreviousPeriod.Count);
        Assert.All(result.PreviousPeriod, point =>
        {
            Assert.Equal(0m, point.TotalSpend);
            Assert.Equal(0, point.TransactionCount);
        });
        var fifth = result.CurrentPeriod[4];
        Assert.Equal(new DateOnly(2026, 8, 5), fifth.Date);
        Assert.Equal(140.00m, fifth.TotalSpend);
        Assert.Equal(2, fifth.TransactionCount);
        var sixth = result.CurrentPeriod[5];
        Assert.Equal(new DateOnly(2026, 8, 6), sixth.Date);
        Assert.Equal(0m, sixth.TotalSpend);
        Assert.Equal(1, sixth.TransactionCount);
        Assert.Equal(0m, result.CurrentPeriod[0].TotalSpend);
        Assert.Equal(0, result.CurrentPeriod[0].TransactionCount);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_WeeklyPeriod_ReturnsSevenDailyPointsPerWeek()
    {
        // Arrange
        var account = AccountFactory.Create(AuthenticatedUserId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Current week debit", date: new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Previous week debit", date: new DateTimeOffset(2026, 7, 31, 0, 0, 0, TimeSpan.Zero), amount: 50.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>("/api/transactions/dailySummary?recordedDate=2026-08-05&timePeriod=Weekly", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 3), result.CurrentStart);
        Assert.Equal(new DateOnly(2026, 8, 10), result.CurrentEnd);
        Assert.Equal(new DateOnly(2026, 7, 27), result.PreviousStart);
        Assert.Equal(new DateOnly(2026, 8, 3), result.PreviousEnd);
        Assert.Equal(7, result.CurrentPeriod.Count);
        Assert.Equal(7, result.PreviousPeriod.Count);
        Assert.Equal(0m, result.CurrentPeriod[0].TotalSpend);
        Assert.Equal(new DateOnly(2026, 8, 9), result.CurrentPeriod[6].Date);
        Assert.Equal(100.00m, result.CurrentPeriod[6].TotalSpend);
        Assert.Equal(1, result.CurrentPeriod[6].TransactionCount);
        Assert.Equal(new DateOnly(2026, 7, 31), result.PreviousPeriod[4].Date);
        Assert.Equal(50.00m, result.PreviousPeriod[4].TotalSpend);
        Assert.Equal(1, result.PreviousPeriod[4].TransactionCount);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_NoTransactions_ReturnsAllZeroPoints()
    {
        // Arrange
        const string parameters = "recordedDate=2026-08-05&timePeriod=Weekly";

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(7, result.CurrentPeriod.Count);
        Assert.Equal(7, result.PreviousPeriod.Count);
        Assert.All(result.CurrentPeriod, point =>
        {
            Assert.Equal(0m, point.TotalSpend);
            Assert.Equal(0, point.TransactionCount);
        });
        Assert.All(result.PreviousPeriod, point =>
        {
            Assert.Equal(0m, point.TotalSpend);
            Assert.Equal(0, point.TransactionCount);
        });
    }

    [Fact]
    public async Task GetDailyTransactionSummary_UnrecognizedTimePeriod_FallsBackToMonthly()
    {
        // Arrange
        const string parameters = "recordedDate=2026-08-05&timePeriod=Annualish";

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(new DateOnly(2026, 8, 1), result.CurrentStart);
        Assert.Equal(31, result.CurrentPeriod.Count);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_FilterByAccountId_ReturnsFilteredPoints()
    {
        // Arrange
        var firstAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Account A");
        var otherAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Account B");
        await DbContext.SeedRangeAsync([firstAccount, otherAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, firstAccount.AccountId, description: "Daily Account A debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, otherAccount.AccountId, description: "Daily Account B debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?recordedDate=2026-08-05&accountId={firstAccount.AccountId}", CancellationToken);

        // Assert
        Assert.Equal(31, result.CurrentPeriod.Count);
        Assert.Equal(100.00m, result.CurrentPeriod[4].TotalSpend);
        Assert.Equal(1, result.CurrentPeriod[4].TransactionCount);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_FilterByInstitutionId_ReturnsFilteredPoints()
    {
        // Arrange
        var wellsFargoAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Wells Fargo Account");
        var chaseAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Chase Account", institutionId: TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync([wellsFargoAccount, chaseAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, wellsFargoAccount.AccountId, description: "Wells Fargo debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, chaseAccount.AccountId, description: "Chase debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?recordedDate=2026-08-05&institutionId={TestConstants.ChaseInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(31, result.CurrentPeriod.Count);
        Assert.Equal(new DateOnly(2026, 8, 5), result.CurrentPeriod[4].Date);
        Assert.Equal(40.00m, result.CurrentPeriod[4].TotalSpend);
        Assert.Equal(1, result.CurrentPeriod[4].TransactionCount);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_InstitutionWithNoAccounts_ReturnsZeroedDailyPoints()
    {
        // Arrange
        var parameters = $"recordedDate=2026-08-05&timePeriod=Weekly&institutionId={TestConstants.NonExistentInstitutionId}";

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?{parameters}", CancellationToken);

        // Assert
        Assert.Equal(7, result.CurrentPeriod.Count);
        Assert.Equal(7, result.PreviousPeriod.Count);
        Assert.All(result.CurrentPeriod, point =>
        {
            Assert.Equal(0m, point.TotalSpend);
            Assert.Equal(0, point.TransactionCount);
        });
        Assert.All(result.PreviousPeriod, point =>
        {
            Assert.Equal(0m, point.TotalSpend);
            Assert.Equal(0, point.TransactionCount);
        });
    }

    [Fact]
    public async Task GetDailyTransactionSummary_InstitutionFilter_ExcludesTransactionsFromInactiveAccounts()
    {
        // Arrange
        var activeAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Active Account");
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Inactive Account");
        inactiveAccount.SetIsActive(false);
        await DbContext.SeedRangeAsync([activeAccount, inactiveAccount], CancellationToken);
        var transactions = new[]
        {
            TransactionFactory.Create(AuthenticatedUserId, activeAccount.AccountId, description: "Active account debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 100.00m),
            TransactionFactory.Create(AuthenticatedUserId, inactiveAccount.AccountId, description: "Inactive account debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m)
        };
        await DbContext.SeedRangeAsync(transactions, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?recordedDate=2026-08-05&institutionId={TestConstants.WellsFargoInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(100.00m, result.CurrentPeriod[4].TotalSpend);
        Assert.Equal(1, result.CurrentPeriod[4].TransactionCount);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_InstitutionFilter_ExcludesTransactionsFromInactiveInstitution()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = AccountFactory.Create(AuthenticatedUserId, accountName: "Daily Inactive Institution Account", institutionId: inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(account, CancellationToken);
        var transaction = TransactionFactory.Create(AuthenticatedUserId, account.AccountId, description: "Inactive institution debit", date: new DateTimeOffset(2026, 8, 5, 0, 0, 0, TimeSpan.Zero), amount: 40.00m);
        await DbContext.SeedAsync(transaction, CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>($"/api/transactions/dailySummary?recordedDate=2026-08-05&institutionId={inactiveInstitution.InstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(0m, result.CurrentPeriod[4].TotalSpend);
        Assert.Equal(0, result.CurrentPeriod[4].TransactionCount);
    }

    [Fact]
    public async Task GetDailyTransactionSummary_TransactionsFromOtherUsers_AreExcluded()
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
        var result = await HttpClient.GetResultAsync<DailySummaryResponseDto>("/api/transactions/dailySummary?recordedDate=2026-08-05", CancellationToken);

        // Assert
        Assert.Equal(31, result.CurrentPeriod.Count);
        Assert.Equal(100.00m, result.CurrentPeriod[4].TotalSpend);
        Assert.Equal(1, result.CurrentPeriod[4].TransactionCount);
    }
}
