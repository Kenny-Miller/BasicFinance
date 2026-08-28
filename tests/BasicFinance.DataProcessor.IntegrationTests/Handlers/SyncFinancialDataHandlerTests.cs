using BasicFinance.DataProcessor.IntegrationTests.Constants;
using BasicFinance.DataProcessor.IntegrationTests.Factory;
using BasicFinance.DataProcessor.IntegrationTests.Infrastructure;
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
        _ = await Host
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
        Assert.Equal((int)AccountType.Checking, account.AccountTypeId);

        var ledgerEntry = await DbContext.AccountLedgers
            .FirstOrDefaultAsync(l => l.AccountId == account!.AccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(ledgerEntry);
        Assert.Equal(1000m, ledgerEntry!.Balance);
    }

    [Fact]
    public async Task Handle_ValidSpreadsheetWithCreditCardAccount_StoresOpeningBalanceAsNegative()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson(accountType: "Credit");

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Test Credit Card",
                800m,
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
        var account = await DbContext.Accounts
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(account);
        Assert.Equal("Test Credit Card", account.AccountName);
        Assert.Equal((int)AccountType.CreditCard, account.AccountTypeId);

        var ledgerEntry = await DbContext.AccountLedgers
            .FirstOrDefaultAsync(l => l.AccountId == account!.AccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(ledgerEntry);
        Assert.Equal(-800m, ledgerEntry!.Balance);
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
    public async Task Handle_AccountRemovedFromSpreadsheet_SoftClosesAccount()
    {
        // Arrange - seed an account that will be removed from the spreadsheet
        var financialAccountId = Guid.NewGuid();

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.Checking,
            TestConstants.TestUserId,
            "To Be Closed",
            500m,
            "USD",
            "Will be closed",
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
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(account);
        Assert.False(account!.IsActive);
        Assert.NotNull(account.SystemModifiedDate);

        var ledgerEntry = await DbContext.AccountLedgers
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.AccountId == account.AccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(ledgerEntry);
        Assert.Equal(500m, ledgerEntry!.Balance);
    }

    [Fact]
    public async Task Handle_ExistingAccountWithUnchangedBalance_DoesNotAppendLedgerEntry()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var lastUpdatedDate = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.Checking,
            TestConstants.TestUserId,
            "Unchanged Checking",
            1000m,
            "USD",
            "Balance unchanged",
            institution.InstitutionId,
            financialAccountId,
            lastUpdatedDate);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Unchanged Checking",
                1000m,
                "USD",
                "Balance unchanged",
                lastUpdatedDate,
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

        var ledgerEntries = await DbContext.AccountLedgers
            .Where(l => l.AccountId == seedAccount.AccountId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var entry = Assert.Single(ledgerEntries);
        Assert.Equal(1000m, entry.Balance);
    }

    [Fact]
    public async Task Handle_ExistingAccountWithChangedBalance_AppendsLedgerEntry()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var initialBalanceDate = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        var updatedBalanceDate = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.Checking,
            TestConstants.TestUserId,
            "Changed Checking",
            1000m,
            "USD",
            "Balance changed",
            institution.InstitutionId,
            financialAccountId,
            initialBalanceDate);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Changed Checking",
                750m,
                "USD",
                "Balance changed",
                updatedBalanceDate,
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

        var ledgerEntries = await DbContext.AccountLedgers
            .Where(l => l.AccountId == seedAccount.AccountId)
            .OrderBy(l => l.BalanceRecordedDate)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, ledgerEntries.Count);
        Assert.Equal(1000m, ledgerEntries[0].Balance);
        Assert.Equal(750m, ledgerEntries[1].Balance);
    }

    [Fact]
    public async Task Handle_CreditCardAccountWithUnchangedBalance_DoesNotAppendLedgerEntry()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var lastUpdatedDate = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson(accountType: "Credit");

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.CreditCard,
            TestConstants.TestUserId,
            "Unchanged Credit Card",
            -1000m,
            "USD",
            "Balance unchanged",
            institution.InstitutionId,
            financialAccountId,
            lastUpdatedDate);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Unchanged Credit Card",
                1000m,
                "USD",
                "Balance unchanged",
                lastUpdatedDate,
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

        var ledgerEntries = await DbContext.AccountLedgers
            .Where(l => l.AccountId == seedAccount.AccountId)
            .ToListAsync(TestContext.Current.CancellationToken);

        var entry = Assert.Single(ledgerEntries);
        Assert.Equal(-1000m, entry.Balance);
    }

    [Fact]
    public async Task Handle_CreditCardAccountWithChangedBalance_AppendsNegativeLedgerEntry()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var initialBalanceDate = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        var updatedBalanceDate = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson(accountType: "Credit");

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.CreditCard,
            TestConstants.TestUserId,
            "Changed Credit Card",
            -500m,
            "USD",
            "Balance changed",
            institution.InstitutionId,
            financialAccountId,
            initialBalanceDate);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Changed Credit Card",
                750m,
                "USD",
                "Balance changed",
                updatedBalanceDate,
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

        var ledgerEntries = await DbContext.AccountLedgers
            .Where(l => l.AccountId == seedAccount.AccountId)
            .OrderBy(l => l.BalanceRecordedDate)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, ledgerEntries.Count);
        Assert.Equal(-500m, ledgerEntries[0].Balance);
        Assert.Equal(-750m, ledgerEntries[1].Balance);
    }

    [Fact]
    public async Task Handle_SoftClosedAccountReappears_ReactivatesWithoutNewLedgerEntry()
    {
        // Arrange
        var financialAccountId = Guid.NewGuid();
        var lastUpdatedDate = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        var rawDataJson = GoogleSpreadsheetExportFactory.CreateAccountExportJson();

        var userSpreadsheet = await DbContext.UserGoogleSpreadsheets
            .FirstAsync(u => u.UserGoogleSpreadsheetId == TestConstants.TestUserGoogleSpreadsheetId, TestContext.Current.CancellationToken);

        var institution = await DbContext.Institutions
            .FirstAsync(i => i.Name == "Wells Fargo", TestContext.Current.CancellationToken);

        var seedAccount = AccountFactory.Create(
            userSpreadsheet.UserGoogleSpreadsheetId,
            AccountType.Checking,
            TestConstants.TestUserId,
            "Reappeared Checking",
            500m,
            "USD",
            "Was previously closed",
            institution.InstitutionId,
            financialAccountId,
            lastUpdatedDate);

        seedAccount.SetIsActive(false);

        DbContext.Accounts.Add(seedAccount);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var response = new SpreadsheetDataFactory()
            .AddAccountRow(
                "Reappeared Checking",
                500m,
                "USD",
                "Was previously closed",
                lastUpdatedDate,
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
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.FinancialAccountId == financialAccountId, TestContext.Current.CancellationToken);

        Assert.NotNull(account);
        Assert.True(account!.IsActive);

        var entry = Assert.Single(await DbContext.AccountLedgers
            .AsNoTracking()
            .Where(l => l.AccountId == account.AccountId)
            .ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(500m, entry.Balance);
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