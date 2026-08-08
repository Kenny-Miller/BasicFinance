using BasicFinance.DataProcessor.IntegrationTests.Constants;
using BasicFinance.DataProcessor.IntegrationTests.Factory;
using BasicFinance.DataProcessor.IntegrationTests.InfrastructureV2;
using BasicFinance.Domain.Commands;
using Google.Apis.Sheets.v4.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Wolverine.Tracking;
using Xunit;
using AccountType = BasicFinance.Infrastructure.Enums.AccountType;
using TransactionType = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.DataProcessor.IntegrationTests.Handlers;

public class SyncFinancialDataHandlerTests : DataProcessorTestFixtureBase
{
    public SyncFinancialDataHandlerTests(DataProcessorClassFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_SpreadsheetNotFound_ReturnsWithoutProcessing()
    {
        // Arrange
        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>())
            .Returns((BatchGetValuesResponse?)null);

        var command = new SyncFinancialData(TestConstants.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        var accounts = await DbContext.Accounts.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Empty(accounts);
    }

    [Fact]
    public async Task Handle_ValidSpreadsheetWithAccount_CreatesAccount()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Test Checking",
                1000m,
                "USD",
                "Test notes",
                DateTime.UtcNow,
                "Wells Fargo",
                financialAccountId,
                rawDataJson)
            .Build();

        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(response);

        var command = new SyncFinancialData(TestConstants.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(account);
        Assert.Equal("Test Checking", account.AccountName);
        Assert.Equal(1000m, account.Balance);
        Assert.Equal((int)AccountType.Checking, account.AccountTypeId);
    }

    [Fact]
    public async Task Handle_ValidSpreadsheetWithTransaction_CreatesTransaction()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var accountRawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();
        var transactionRawDataJson = GoogleSpreadsheetExportFactory.CreateTransactionExportJson(12345);

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Test Checking",
                1000m,
                "USD",
                "Test notes",
                DateTime.UtcNow,
                "Wells Fargo",
                financialAccountId,
                accountRawDataJson)
            .AddTransactionRow(
                DateTime.UtcNow,
                50m,
                "Test Purchase",
                "Uncategorized",
                "Test Checking",
                transactionRawDataJson)
            .Build();

        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(response);

        var command = new SyncFinancialData(TestConstants.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.FinancialTransactionId == 12345, TestContext.Current.CancellationToken);

        Assert.NotNull(transaction);
        Assert.Equal("Test Purchase", transaction.Description);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal((int)TransactionType.Debit, transaction.TransactionTypeId);
    }

    [Fact]
    public async Task Handle_RemoveAccountFromSpreadsheet_HardDeletesAccount()
    {
        // Arrange - seed an account that will be removed
        var financialAccountId = Guid.NewGuid();
        var accountRawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.Checking,
            TestConstants.TestUserId,
            "To Be Removed",
            500m,
            "USD",
            "Will be removed",
            institution.InstitutionId,
            financialAccountId,
            DateTime.UtcNow);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - send empty spreadsheet (no accounts)
        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>())
            .Returns(new SpreadsheetDataFactory().Build());

        var command = new SyncFinancialData(TestConstants.TestUserGoogleSpreadsheetId);

        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId, TestContext.Current.CancellationToken);

        Assert.Null(account);
    }

    [Fact]
    public async Task Handle_InvalidInstitution_SkipsAccount()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Unknown Account",
                1000m,
                "USD",
                "Test notes",
                DateTime.UtcNow,
                "Unknown Institution",
                financialAccountId,
                rawDataJson)
            .Build();

        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(response);

        var command = new SyncFinancialData(TestConstants.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<string>>(),
            Arg.Any<CancellationToken>());

        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId, TestContext.Current.CancellationToken);

        Assert.Null(account);
    }
}
