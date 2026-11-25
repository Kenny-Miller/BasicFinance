using BasicFinance.Api.IntegrationTests.Helpers;
using BasicFinance.Api.IntegrationTests.Infrastructure.Extensions;
using BasicFinance.Api.IntegrationTests.Infrastructure.Factories;
using BasicFinance.Api.IntegrationTests.Infrastructure.Fixtures;
using BasicFinance.Infrastructure.Entities;
using Xunit;
using TransactionCategoryEnum = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionTypeEnum = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.Api.IntegrationTests.Features.Insights;

public class GetCashflowOverTimeTests : ApiTestFixtureBase
{
    private const string Endpoint = "/api/insights/cashflow-over-time";

    private const string OtherUserId = "11111111-1111-1111-1111-111111111111";

    public GetCashflowOverTimeTests(ApiClassFixture fixture)
        : base(fixture)
    {
    }

    private static DateTime CurrentMonth => new(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static DateTimeOffset Utc(int year, int month, int day) => new(year, month, day, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCashflowOverTime_NoTransactions_ReturnsZeroFilledMonthsEndingAtCurrentMonthWithNoNeighbouringPages()
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=SixMonths", CancellationToken);

        // Assert
        Assert.Equal(6, result.CashflowTimeline.Count);
        var expectedMonths = Enumerable.Range(0, 6).Select(i => CurrentMonth.AddMonths(i - 5)).Select(m => (m.Year, m.Month));
        Assert.Equal(expectedMonths, result.CashflowTimeline.Select(x => (x.Year, x.Month)));
        Assert.All(result.CashflowTimeline, x =>
        {
            Assert.Equal(0m, x.Income);
            Assert.Equal(0m, x.Spend);
            Assert.Equal(0m, x.Net);
        });
        Assert.Equal(0m, result.NetCashflow);
        Assert.Equal(CurrentMonth.AddMonths(-5), result.Page.WindowStart);
        Assert.Equal(CurrentMonth.AddMonths(1).AddDays(-1), result.Page.WindowEnd);
        Assert.Null(result.Page.PreviousAsOf);
        Assert.Null(result.Page.NextAsOf);
    }

    [Theory]
    [InlineData("OneMonth", 1)]
    [InlineData("ThreeMonths", 3)]
    [InlineData("SixMonths", 6)]
    [InlineData("OneYear", 12)]
    public async Task GetCashflowOverTime_EachRange_ReturnsThatManyWholeMonthsWhateverDayIsRequested(string range, int expectedMonths)
    {
        // Act
        var midMonth = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range={range}&asOf=2025-10-17", CancellationToken);
        var monthEnd = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range={range}&asOf=2025-10-31", CancellationToken);

        // Assert
        Assert.Equal(expectedMonths, midMonth.CashflowTimeline.Count);
        Assert.Equal((2025, 10), (midMonth.CashflowTimeline[^1].Year, midMonth.CashflowTimeline[^1].Month));
        Assert.Equal(midMonth.CashflowTimeline, monthEnd.CashflowTimeline);
        Assert.Equal(new DateTime(2025, 10, 31, 0, 0, 0, DateTimeKind.Unspecified), midMonth.Page.WindowEnd);
    }

    [Fact]
    public async Task GetCashflowOverTime_AllRange_StartsAtMonthOfEarliestTransactionWithNoPreviousPage()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 1, 10), amount: 10m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 3, 10), amount: 30m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 6, 1), amount: 99m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=All&asOf=2025-05-20", CancellationToken);

        // Assert
        Assert.Equal([(2025, 1), (2025, 2), (2025, 3), (2025, 4), (2025, 5)], result.CashflowTimeline.Select(x => (x.Year, x.Month)));
        Assert.Equal(40m, result.TotalSpend);
        Assert.Equal(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Unspecified), result.Page.WindowStart);
        Assert.Equal(new DateTime(2025, 5, 31, 0, 0, 0, DateTimeKind.Unspecified), result.Page.WindowEnd);
        Assert.Null(result.Page.PreviousAsOf);
    }

    [Fact]
    public async Task GetCashflowOverTime_AllRangeWithNoTransactions_ReturnsSingleZeroMonth()
    {
        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=All&asOf=2025-05-20", CancellationToken);

        // Assert
        var month = Assert.Single(result.CashflowTimeline);
        Assert.Equal(new InsightsMonthlyCashflowDto(2025, 5, 0m, 0m, 0m), month);
        Assert.Null(result.Page.PreviousAsOf);
    }

    [Fact]
    public async Task GetCashflowOverTime_CreditsAndDebits_CreditIsIncomeAndDebitIsSpend()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Credit, TransactionCategoryEnum.Income, Utc(2025, 10, 1), 1000m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, TransactionCategoryEnum.Groceries, Utc(2025, 10, 3), 100m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, TransactionCategoryEnum.Shopping, Utc(2025, 10, 31), 50m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=OneMonth&asOf=2025-10-01", CancellationToken);

        // Assert
        var month = Assert.Single(result.CashflowTimeline);
        Assert.Equal(new InsightsMonthlyCashflowDto(2025, 10, 1000m, 150m, 850m), month);
        Assert.Equal(1000m, result.TotalIncome);
        Assert.Equal(150m, result.TotalSpend);
        Assert.Equal(850m, result.NetCashflow);
    }

    [Fact]
    public async Task GetCashflowOverTime_PageAcrossYearBoundary_ReturnsEachMonthWithItsYearAndZeroFillsGaps()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, date: Utc(2025, 12, 31), amount: 200m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Credit, TransactionCategoryEnum.Income, Utc(2026, 1, 1), 500m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf=2026-01-15", CancellationToken);

        // Assert
        Assert.Equal(
            [
                new InsightsMonthlyCashflowDto(2025, 11, 0m, 0m, 0m),
                new InsightsMonthlyCashflowDto(2025, 12, 0m, 200m, -200m),
                new InsightsMonthlyCashflowDto(2026, 1, 500m, 0m, 500m),
            ],
            result.CashflowTimeline);
        Assert.Equal(300m, result.NetCashflow);
    }

    [Fact]
    public async Task GetCashflowOverTime_OlderTransactionsExist_PreviousAsOfPagesToAdjacentMonthsAndBack()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 1, 10), amount: 10m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 3, 10), amount: 30m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 5, 10), amount: 50m),
            ],
            CancellationToken);

        // Act
        var firstPage = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf=2025-06-01", CancellationToken);
        var olderPage = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf={firstPage.Page.PreviousAsOf:yyyy-MM-dd}", CancellationToken);
        var backAgain = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf={olderPage.Page.NextAsOf:yyyy-MM-dd}", CancellationToken);

        // Assert
        Assert.Equal(new DateTime(2025, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), firstPage.Page.PreviousAsOf);
        Assert.Equal(50m, firstPage.TotalSpend);
        Assert.Equal([(2025, 1), (2025, 2), (2025, 3)], olderPage.CashflowTimeline.Select(x => (x.Year, x.Month)));
        Assert.Equal(40m, olderPage.TotalSpend);
        Assert.Null(olderPage.Page.PreviousAsOf);
        Assert.Equal(new DateTime(2025, 6, 30, 0, 0, 0, DateTimeKind.Unspecified), olderPage.Page.NextAsOf);
        Assert.Equal(firstPage.CashflowTimeline, backAgain.CashflowTimeline);
    }

    [Fact]
    public async Task GetCashflowOverTime_NoOlderTransactions_PreviousAsOfIsNull()
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedAsync(TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 4, 1)), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf=2025-06-01", CancellationToken);

        // Assert
        Assert.Null(result.Page.PreviousAsOf);
    }

    [Fact]
    public async Task GetCashflowOverTime_PastAsOf_NextAsOfIsCappedAtToday()
    {
        // Arrange
        var previousMonth = CurrentMonth.AddMonths(-1);
        var today = DateTimeOffset.UtcNow.UtcDateTime.Date;

        // Act
        var nearPage = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=SixMonths&asOf={previousMonth:yyyy-MM-dd}", CancellationToken);
        var farPage = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf=2025-01-01", CancellationToken);

        // Assert
        Assert.Equal(today, nearPage.Page.NextAsOf);
        Assert.Equal(new DateTime(2025, 4, 30, 0, 0, 0, DateTimeKind.Unspecified), farPage.Page.NextAsOf);
    }

    [Fact]
    public async Task GetCashflowOverTime_FutureAsOf_IsClampedToCurrentMonth()
    {
        // Arrange
        var futureMonth = CurrentMonth.AddMonths(3);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=ThreeMonths&asOf={futureMonth:yyyy-MM-dd}", CancellationToken);

        // Assert
        var last = result.CashflowTimeline[^1];
        Assert.Equal((CurrentMonth.Year, CurrentMonth.Month), (last.Year, last.Month));
        Assert.Null(result.Page.NextAsOf);
    }

    [Theory]
    [InlineData(TransactionCategoryEnum.InternalTransfer)]
    [InlineData(TransactionCategoryEnum.SavingsTransfer)]
    [InlineData(TransactionCategoryEnum.CreditCardPayment)]
    [InlineData(TransactionCategoryEnum.Ignore)]
    public async Task GetCashflowOverTime_TransferCategories_AreExcluded(TransactionCategoryEnum category)
    {
        // Arrange
        var account = await SeedAccountAsync();
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, category, Utc(2025, 10, 1), 500m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Credit, category, Utc(2025, 10, 1), 500m),
                TransactionFactory.Create(AuthenticatedUserId, account.AccountId, TransactionTypeEnum.Debit, TransactionCategoryEnum.Groceries, Utc(2025, 10, 2), 25m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=OneMonth&asOf=2025-10-01", CancellationToken);

        // Assert
        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(25m, result.TotalSpend);
    }

    [Fact]
    public async Task GetCashflowOverTime_InstitutionFilter_OnlyIncludesAccountsAtInstitution()
    {
        // Arrange
        var wellsFargo = await SeedAccountAsync();
        var chase = await SeedAccountAsync(TestConstants.ChaseInstitutionId);
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, wellsFargo.AccountId, date: Utc(2025, 10, 1), amount: 100m),
                TransactionFactory.Create(AuthenticatedUserId, chase.AccountId, date: Utc(2025, 10, 1), amount: 7m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=OneMonth&asOf=2025-10-01&institutionId={TestConstants.ChaseInstitutionId}", CancellationToken);

        // Assert
        Assert.Equal(7m, result.TotalSpend);
    }

    [Fact]
    public async Task GetCashflowOverTime_TransactionsFromOtherUsers_AreExcluded()
    {
        // Arrange
        var ownAccount = await SeedAccountAsync();
        var otherAccount = AccountFactory.Create(OtherUserId);
        await DbContext.SeedAsync(otherAccount, CancellationToken);
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, ownAccount.AccountId, date: Utc(2025, 10, 1), amount: 10m),
                TransactionFactory.Create(OtherUserId, otherAccount.AccountId, date: Utc(2025, 10, 1), amount: 999m),
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=OneMonth&asOf=2025-10-01", CancellationToken);

        // Assert
        Assert.Equal(10m, result.TotalSpend);
    }

    [Fact]
    public async Task GetCashflowOverTime_InactiveTransactionsAndAccounts_AreExcluded()
    {
        // Arrange
        var activeAccount = await SeedAccountAsync();
        var inactiveAccount = AccountFactory.Create(AuthenticatedUserId, accountName: "Inactive");
        inactiveAccount.SetIsActive(false);
        await DbContext.SeedAsync(inactiveAccount, CancellationToken);
        var inactiveTransaction = TransactionFactory.Create(AuthenticatedUserId, activeAccount.AccountId, date: Utc(2025, 10, 1), amount: 300m);
        inactiveTransaction.IsActive = false;
        await DbContext.SeedRangeAsync(
            [
                TransactionFactory.Create(AuthenticatedUserId, activeAccount.AccountId, date: Utc(2025, 10, 1), amount: 10m),
                TransactionFactory.Create(AuthenticatedUserId, inactiveAccount.AccountId, date: Utc(2025, 10, 1), amount: 200m),
                inactiveTransaction,
            ],
            CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=OneMonth&asOf=2025-10-01", CancellationToken);

        // Assert
        Assert.Equal(10m, result.TotalSpend);
    }

    [Fact]
    public async Task GetCashflowOverTime_AccountsFromInactiveInstitution_AreExcluded()
    {
        // Arrange
        var inactiveInstitution = InstitutionFactory.Create(name: "Inactive Bank", institutionCode: "INACT");
        await DbContext.SeedAsync(inactiveInstitution, CancellationToken);
        inactiveInstitution.IsActive = false;
        await DbContext.SaveChangesAsync(CancellationToken);
        var account = await SeedAccountAsync(inactiveInstitution.InstitutionId);
        await DbContext.SeedAsync(TransactionFactory.Create(AuthenticatedUserId, account.AccountId, date: Utc(2025, 10, 1), amount: 10m), CancellationToken);

        // Act
        var result = await HttpClient.GetResultAsync<InsightsCashflowOverTimeResponseDto>($"{Endpoint}?range=OneMonth&asOf=2025-10-01", CancellationToken);

        // Assert
        Assert.Equal(0m, result.TotalSpend);
    }

    private async Task<Account> SeedAccountAsync(int institutionId = TestConstants.WellsFargoInstitutionId)
    {
        var account = AccountFactory.Create(AuthenticatedUserId, institutionId: institutionId);
        return await DbContext.SeedAsync(account, CancellationToken);
    }
}
