using BasicFinance.DataProcessor.IntegrationTests.Helpers;
using BasicFinance.DataProcessor.IntegrationTests.InfrastructureV2;
using BasicFinance.Domain.Commands;
using BasicFinance.Infrastructure.Entities;
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
        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns((BatchGetValuesResponse?)null);

        var command = new SyncFinancialData(DbDataHelper.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());

        var accounts = await DbContext.Accounts.ToListAsync();
        Assert.Empty(accounts);
    }

    [Fact]
    public async Task Handle_ValidSpreadsheetWithAccount_CreatesAccount()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var rawDataJson = WellsFargoExportHelpers.CreateAccountExportJson();

        var response = new SpreadsheetDataBuilder()
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

        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(response);

        var command = new SyncFinancialData(DbDataHelper.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());

        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId);

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
        var accountRawDataJson = WellsFargoExportHelpers.CreateAccountExportJson();
        var transactionRawDataJson = WellsFargoExportHelpers.CreateTransactionExportJson(12345);

        var response = new SpreadsheetDataBuilder()
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

        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(response);

        var command = new SyncFinancialData(DbDataHelper.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());

        var transaction = await DbContext.Transactions
            .FirstOrDefaultAsync(t => t.FinancialTransactionId == 12345);

        Assert.NotNull(transaction);
        Assert.Equal("Test Purchase", transaction.Description);
        Assert.Equal(50m, transaction.Amount);
        Assert.Equal((int)TransactionType.Debit, transaction.TransactionTypeId);
    }

    [Fact]
    public async Task Handle_RemoveAccountFromSpreadsheet_DeactivatesAccount()
    {
        // Arrange - seed an account that will be removed
        var financialAccountId = Guid.NewGuid();
        var accountRawDataJson = WellsFargoExportHelpers.CreateAccountExportJson();

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == DbDataHelper.TestUserGoogleSpreadsheetId);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo");

        var seedAccount = new Account(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.Checking,
            DbDataHelper.TestUserId,
            "To Be Removed",
            500m,
            "USD",
            "Will be removed",
            institution.InstitutionId,
            financialAccountId,
            DateTime.UtcNow);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync();

        // Act - send empty spreadsheet (no accounts)
        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(new SpreadsheetDataBuilder().Build());

        var command = new SyncFinancialData(DbDataHelper.TestUserGoogleSpreadsheetId);

        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId);

        Assert.NotNull(account);
        Assert.False(account.IsActive);
    }

    [Fact]
    public async Task Handle_InvalidInstitution_SkipsAccount()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var rawDataJson = WellsFargoExportHelpers.CreateAccountExportJson();

        var response = new SpreadsheetDataBuilder()
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

        MockGoogleServiceAccountClient.GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(response);

        var command = new SyncFinancialData(DbDataHelper.TestUserGoogleSpreadsheetId);

        // Act
        var result = await Host
            .TrackActivity()
            .IncludeExternalTransports()
            .SendMessageAndWaitAsync(command);

        // Assert
        await MockGoogleServiceAccountClient.Received(1).GetSubSpreadsheetsAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());

        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId);

        Assert.Null(account);
    }
}
