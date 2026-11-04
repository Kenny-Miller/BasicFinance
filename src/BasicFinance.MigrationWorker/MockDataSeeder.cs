using System.Globalization;
using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

using AccountType = BasicFinance.Infrastructure.Enums.AccountType;
using TransactionCategory = BasicFinance.Infrastructure.Enums.TransactionCategory;
using TransactionType = BasicFinance.Infrastructure.Enums.TransactionType;

namespace BasicFinance.MigrationWorker
{
    /// <summary>
    /// Seeds mock accounts, transactions, and balance ledger snapshots for a fixed developer user,
    /// enabling UI development without a real Google Spreadsheet.
    /// </summary>
    public static partial class MockDataSeeder
    {
        /// <summary>
        /// Gets the id of the user that mock data is seeded for.
        /// </summary>
        public const string TargetUserId = "b08fd9eb-4f65-4ba3-8171-1c4af7484231";

        private const string WellsFargoInstitutionCode = "WF";
        private const string MockGoogleSheetId = "MOCK-SPREADSHEET-001";
        private const string MockGoogleSheetName = "Mock Spreadsheet";
        private const string MockAccountNotes = "Mock account for development";
        private const string DefaultCurrency = "USD";
        private const int SeedYears = 2;
        private const int RandomSeed = 42;
        private const long FinancialTransactionIdBase = 1_000_000L;
        private const int RandomDescriptionSuffixMin = 1000;
        private const int RandomDescriptionSuffixMax = 9999;

        private static readonly Guid MockUserGoogleSpreadsheetId = new("f47ac10b-58cc-4372-a567-0e02b2c3d479");

        private static readonly Guid[] MockFinancialAccountIds =
        [
            new("8f6c8f3b-2a1d-4e5f-9b0a-1c2d3e4f5a6b"),
            new("2b7e41f8-9d3a-4c6e-a1b2-3c4d5e6f7a8c"),
            new("5c1d9a4e-7f2b-4a3c-b6d8-9e0f1a2b3c4d"),
            new("1e0f2a3b-6c5d-4e4f-a9b8-7c6d5e4f3a2b"),
        ];

        private static readonly AccountSpec[] MockAccounts =
        [
            new(AccountType.Checking, "WF Checking", 1_500.00m,
            [
                new(TransactionCategory.Income, true, "Payroll Deposit - Acme Corp", 2_400.00m, 2_800.00m),
                new(TransactionCategory.Income, true, "Payroll Deposit - Acme Corp", 2_400.00m, 2_800.00m),
                new(TransactionCategory.Income, true, "Payroll Deposit - Acme Corp", 2_400.00m, 2_800.00m),
                new(TransactionCategory.Income, true, "Payroll Deposit - Acme Corp", 2_400.00m, 2_800.00m),
                new(TransactionCategory.Income, true, "Payroll Deposit - Acme Corp", 2_400.00m, 2_800.00m),
                new(TransactionCategory.Income, true, "Payroll Deposit - Acme Corp", 2_400.00m, 2_800.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 25.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 25.00m),
                new(TransactionCategory.Groceries, false, "Morton's Market #{0}", 45.00m, 180.00m),
                new(TransactionCategory.Groceries, false, "Morton's Market #{0}", 45.00m, 180.00m),
                new(TransactionCategory.Groceries, false, "Morton's Market #{0}", 45.00m, 180.00m),
                new(TransactionCategory.Groceries, false, "Morton's Market #{0}", 45.00m, 180.00m),
                new(TransactionCategory.Groceries, false, "Morton's Market #{0}", 45.00m, 180.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Chipotle #{0}", 9.00m, 38.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Chipotle #{0}", 9.00m, 38.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Starbucks #{0}", 6.00m, 14.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Starbucks #{0}", 6.00m, 14.00m),
                new(TransactionCategory.BillsAndUtilities, false, "City Power & Light", 90.00m, 240.00m),
                new(TransactionCategory.BillsAndUtilities, false, "City Power & Light", 90.00m, 240.00m),
                new(TransactionCategory.BillsAndUtilities, false, "City Power & Light", 90.00m, 240.00m),
                new(TransactionCategory.AutoAndTransport, false, "Shell #{0}", 30.00m, 65.00m),
                new(TransactionCategory.AutoAndTransport, false, "Shell #{0}", 30.00m, 65.00m),
                new(TransactionCategory.EntertainmentAndRec, false, "AMC Theatres", 15.00m, 45.00m),
                new(TransactionCategory.Fees, false, "Monthly Service Fee", 0.25m, 5.00m),
                new(TransactionCategory.AutoAndTransport, false, "Metro Transit Pass", 20.00m, 30.00m),
            ]),
            new(AccountType.Savings, "WF Savings", 5_000.00m,
            [
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.Income, true, "Interest Payment", 5.00m, 30.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, false, "Transfer to Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
                new(TransactionCategory.SavingsTransfer, true, "Transfer from Checking", 200.00m, 600.00m),
            ]),
            new(AccountType.CreditCard, "WF Credit Card", -450.00m,
            [
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.Shopping, false, "Amazon #{0}", 15.00m, 150.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Olive Garden #{0}", 40.00m, 120.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Olive Garden #{0}", 40.00m, 120.00m),
                new(TransactionCategory.DiningAndDrinks, false, "Olive Garden #{0}", 40.00m, 120.00m),
                new(TransactionCategory.TravelAndVacation, false, "Delta Air Lines #{0}", 200.00m, 900.00m),
                new(TransactionCategory.TravelAndVacation, false, "Delta Air Lines #{0}", 200.00m, 900.00m),
                new(TransactionCategory.Shopping, false, "Best Buy #{0}", 50.00m, 300.00m),
                new(TransactionCategory.Shopping, false, "Best Buy #{0}", 50.00m, 300.00m),
                new(TransactionCategory.CreditCardPayment, true, "Credit Card Payment", 150.00m, 400.00m),
                new(TransactionCategory.CreditCardPayment, true, "Credit Card Payment", 150.00m, 400.00m),
                new(TransactionCategory.CreditCardPayment, true, "Credit Card Payment", 150.00m, 400.00m),
                new(TransactionCategory.CreditCardPayment, true, "Credit Card Payment", 150.00m, 400.00m),
                new(TransactionCategory.CreditCardPayment, true, "Credit Card Payment", 150.00m, 400.00m),
                new(TransactionCategory.Income, true, "Cash Back Reward", 5.00m, 50.00m),
            ]),
            new(AccountType.Investment, "WF Investment", 12_000.00m,
            [
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Investment, false, "Contribution - VFINX", 500.00m, 1_500.00m),
                new(TransactionCategory.Income, true, "Dividend Income - SCHD", 25.00m, 120.00m),
                new(TransactionCategory.Income, true, "Dividend Income - SCHD", 25.00m, 120.00m),
                new(TransactionCategory.Income, true, "Dividend Income - SCHD", 25.00m, 120.00m),
                new(TransactionCategory.Income, true, "Dividend Income - SCHD", 25.00m, 120.00m),
                new(TransactionCategory.Income, true, "Dividend Income - SCHD", 25.00m, 120.00m),
                new(TransactionCategory.Investment, true, "Sale Proceeds - VTSAX", 400.00m, 1_200.00m),
                new(TransactionCategory.Investment, true, "Sale Proceeds - VTSAX", 400.00m, 1_200.00m),
                new(TransactionCategory.Investment, true, "Sale Proceeds - VTSAX", 400.00m, 1_200.00m),
                new(TransactionCategory.Investment, true, "Sale Proceeds - VTSAX", 400.00m, 1_200.00m),
                new(TransactionCategory.Investment, true, "Sale Proceeds - VTSAX", 400.00m, 1_200.00m),
                new(TransactionCategory.Investment, true, "Sale Proceeds - VTSAX", 400.00m, 1_200.00m),
                new(TransactionCategory.Fees, false, "Brokerage Fee", 0.10m, 15.00m),
                new(TransactionCategory.Fees, false, "Brokerage Fee", 0.10m, 15.00m),
                new(TransactionCategory.Fees, false, "Brokerage Fee", 0.10m, 15.00m),
                new(TransactionCategory.Fees, false, "Brokerage Fee", 0.10m, 15.00m),
                new(TransactionCategory.Investment, false, "Contribution - VTI", 200.00m, 800.00m),
                new(TransactionCategory.Investment, false, "Contribution - VTI", 200.00m, 800.00m),
            ]),
        ];

        /// <summary>
        /// Seeds mock accounts, transactions, and balance ledger snapshots for <see cref="TargetUserId"/>
        /// if none exist yet.
        /// </summary>
        /// <param name="dbContext">The database context used to persist the mock data.</param>
        /// <param name="logger">The logger used to report progress.</param>
        /// <param name="reseed">When true, any existing mock data for <see cref="TargetUserId"/> is removed before seeding.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public static async Task SeedAsync(AppDbContext dbContext, ILogger logger, bool reseed, CancellationToken cancellationToken)
        {
            if (reseed)
            {
                await RemoveMockDataAsync(dbContext, logger, cancellationToken);
            }

            if (await dbContext.Accounts.AnyAsync(a => a.UserId == TargetUserId, cancellationToken))
            {
                LogMockDataAlreadySeeded(logger, TargetUserId);
                return;
            }

            var institution = await dbContext.Institutions
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InstitutionCode == WellsFargoInstitutionCode, cancellationToken);

            if (institution is null)
            {
                LogMockInstitutionNotFound(logger, WellsFargoInstitutionCode);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var windowStart = new DateTimeOffset(now.Year - SeedYears + 1, 1, 1, 0, 0, 0, now.Offset);
            var spreadsheet = new UserGoogleSpreadsheet(TargetUserId, MockGoogleSheetId, MockGoogleSheetName)
            {
                UserGoogleSpreadsheetId = MockUserGoogleSpreadsheetId,
            };

            var accounts = CreateAccounts(institution.InstitutionId, windowStart);
            dbContext.UserGoogleSpreadsheets.Add(spreadsheet);
            dbContext.Accounts.AddRange(accounts);
            await dbContext.SaveChangesAsync(cancellationToken);

            var transactionsCount = 0;
            var ledgerEntriesCount = 0;
#pragma warning disable S2245
            var random = new Random(RandomSeed);
#pragma warning restore S2245
            for (var index = 0; index < accounts.Count; index++)
            {
                var account = accounts[index];
                var (transactions, snapshots) = GenerateAccountData(account, MockAccounts[index].Specs, index, random, windowStart, now);
                dbContext.Transactions.AddRange(transactions);
                dbContext.AccountLedgers.AddRange(snapshots);
                transactionsCount += transactions.Count;
                ledgerEntriesCount += snapshots.Count;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            LogMockDataSeeded(logger, accounts.Count, transactionsCount, ledgerEntriesCount);
        }

        private static List<Account> CreateAccounts(int institutionId, DateTimeOffset windowStart)
        {
            var accounts = new List<Account>(MockAccounts.Length);
            for (var index = 0; index < MockAccounts.Length; index++)
            {
                var spec = MockAccounts[index];
                accounts.Add(new Account(
                    MockUserGoogleSpreadsheetId,
                    spec.AccountType,
                    TargetUserId,
                    spec.Name,
                    DefaultCurrency,
                    MockAccountNotes,
                    institutionId,
                    MockFinancialAccountIds[index],
                    spec.OpeningBalance,
                    windowStart));
            }

            return accounts;
        }

        private static (List<Transaction> Transactions, List<AccountLedger> Snapshots) GenerateAccountData(
            Account account,
            TransactionSpec[] specs,
            int accountIndex,
            Random random,
            DateTimeOffset windowStart,
            DateTimeOffset now)
        {
            var segments = BuildYearSegments(windowStart, now);
            var nextFinancialTransactionId = FinancialTransactionIdBase * (accountIndex + 1L);
            var transactions = new List<Transaction>(specs.Length * segments.Length);
            for (var index = 0; index < specs.Length; index++)
            {
                var spec = specs[index];
                for (var period = 0; period < segments.Length; period++)
                {
                    var segment = segments[period];
                    if (random.NextDouble() >= segment.Fraction)
                    {
                        continue;
                    }

                    var date = segment.Start.AddTicks((long)((segment.End - segment.Start).Ticks * random.NextDouble()));
                    var amount = RandomAmount(random, spec.MinAmount, spec.MaxAmount);
                    var description = GetDescription(spec.Description, random);
                    var financialTransactionId = nextFinancialTransactionId++;
                    transactions.Add(new Transaction(
                        TargetUserId,
                        account.AccountId,
                        financialTransactionId,
                        spec.IsCredit ? TransactionType.Credit : TransactionType.Debit,
                        spec.Category,
                        date,
                        amount,
                        description));
                }
            }

            transactions.Sort(static (a, b) => a.Date.CompareTo(b.Date));
            return (transactions, BuildLedgerSnapshots(account, transactions, windowStart, now));
        }

        /// <summary>
        /// Removes all previously seeded mock data for <see cref="TargetUserId"/>,
        /// so that seeding can start from a clean slate.
        /// </summary>
        /// <param name="dbContext">The database context holding the mock data.</param>
        /// <param name="logger">The logger used to report progress.</param>
        /// <param name="cancellationToken">The token used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task RemoveMockDataAsync(AppDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
        {
            var accountIds = await dbContext.Accounts
                .AsNoTracking()
                .Where(account => account.UserId == TargetUserId)
                .Select(account => account.AccountId)
                .ToListAsync(cancellationToken);

            if (accountIds.Count == 0)
            {
                LogNoMockDataToRemove(logger, TargetUserId);
                return;
            }

            var transactions = await dbContext.Transactions
                .Where(transaction => transaction.UserId == TargetUserId)
                .ToListAsync(cancellationToken);
            var ledgers = await dbContext.AccountLedgers
                .Where(ledger => accountIds.Contains(ledger.AccountId))
                .ToListAsync(cancellationToken);
            var accounts = await dbContext.Accounts
                .Where(account => account.UserId == TargetUserId)
                .ToListAsync(cancellationToken);

            dbContext.Transactions.RemoveRange(transactions);
            dbContext.AccountLedgers.RemoveRange(ledgers);
            dbContext.Accounts.RemoveRange(accounts);

            await dbContext.SaveChangesAsync(cancellationToken);
            LogMockDataRemoved(logger, accounts.Count, transactions.Count, ledgers.Count);
        }

        /// <summary>
        /// Splits the seeding window into calendar-year segments: each full year, plus the (partial) current year.
        /// </summary>
        /// <param name="windowStart">The first instant of the seeding window, aligned to a year start.</param>
        /// <param name="now">The last instant of the seeding window.</param>
        /// <returns>The year segments within the window, in chronological order.</returns>
        private static YearSegment[] BuildYearSegments(DateTimeOffset windowStart, DateTimeOffset now)
        {
            var segments = new List<YearSegment>(now.Year - windowStart.Year + 1);
            for (var year = windowStart.Year; year <= now.Year; year++)
            {
                var fullYearStart = new DateTimeOffset(year, 1, 1, 0, 0, 0, now.Offset);
                var fullYearEnd = fullYearStart.AddYears(1).AddTicks(-1);
                var segmentStart = year == windowStart.Year ? windowStart : fullYearStart;
                var segmentEnd = year == now.Year ? now : fullYearEnd;
                var fullYearTicks = (fullYearEnd - fullYearStart).Ticks + 1;
                var segmentTicks = (segmentEnd - segmentStart).Ticks + 1;
                var fraction = (double)segmentTicks / fullYearTicks;
                segments.Add(new YearSegment(segmentStart, segmentEnd, fraction));
            }

            return [.. segments];
        }

        private static List<AccountLedger> BuildLedgerSnapshots(Account account, List<Transaction> transactions, DateTimeOffset windowStart, DateTimeOffset now)
        {
            var balance = GetOpeningBalance(account);
            var snapshots = new List<AccountLedger>();
            var monthStart = new DateTimeOffset(windowStart.Year, windowStart.Month, 1, 0, 0, 0, windowStart.Offset);
            var cursor = 0;
            while (true)
            {
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                var snapshotDate = monthEnd <= now ? monthEnd : now;
                while (cursor < transactions.Count && transactions[cursor].Date <= snapshotDate)
                {
                    var transaction = transactions[cursor];
                    balance += transaction.TransactionTypeId == (int)TransactionType.Credit
                        ? transaction.Amount
                        : -transaction.Amount;
                    cursor++;
                }

                snapshots.Add(new AccountLedger(account, balance, snapshotDate));
                if (monthEnd >= now)
                {
                    break;
                }

                monthStart = monthStart.AddMonths(1);
            }

            return snapshots;
        }

        private static decimal GetOpeningBalance(Account account) => account.Ledger.First().Balance;

        private static decimal RandomAmount(Random random, decimal min, decimal max)
        {
            var fraction = (decimal)random.NextDouble();
            return Math.Round(min + (max - min) * fraction, 2);
        }

        private static string GetDescription(string template, Random random)
        {
            return template.Contains('{')
                ? string.Format(CultureInfo.InvariantCulture, template, random.Next(RandomDescriptionSuffixMin, RandomDescriptionSuffixMax))
                : template;
        }

        /// <summary>
        /// Describes a mock account type, including its opening balance and its transaction mix.
        /// </summary>
        /// <param name="AccountType">The account type.</param>
        /// <param name="Name">The display name of the account.</param>
        /// <param name="OpeningBalance">The opening balance, stored signed (liabilities are negative).</param>
        /// <param name="Specs">The transactions to generate, one per spec.</param>
        private sealed record AccountSpec(AccountType AccountType, string Name, decimal OpeningBalance, TransactionSpec[] Specs);

        /// <summary>
        /// Describes one transaction row to generate: category, direction, description, and amount range.
        /// Each spec describes a recurring pattern that occurs once per calendar year; duplicate specs
        /// represent a pattern that occurs multiple times per year.
        /// </summary>
        /// <param name="Category">The transaction category.</param>
        /// <param name="IsCredit">Whether the transaction is a credit (money in) or a debit (money out).</param>
        /// <param name="Description">The description template. A {0} placeholder is filled with a random 4-digit number.</param>
        /// <param name="MinAmount">The minimum amount, inclusive.</param>
        /// <param name="MaxAmount">The maximum amount, inclusive.</param>
        private sealed record TransactionSpec(TransactionCategory Category, bool IsCredit, string Description, decimal MinAmount, decimal MaxAmount);

        /// <summary>
        /// A calendar year (or the partial current year) that mock data is generated for,
        /// along with the fraction of a full year it covers.
        /// </summary>
        /// <param name="Start">The first instant of the segment, inclusive.</param>
        /// <param name="End">The last instant of the segment, inclusive.</param>
        /// <param name="Fraction">The fraction of a full year the segment covers, from 0.0 to 1.0.</param>
        private sealed record YearSegment(DateTimeOffset Start, DateTimeOffset End, double Fraction);

        /// <summary>
        /// Logs that mock data already exists for the target user, so seeding is skipped.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userId"></param>
        [LoggerMessage(
            EventName = nameof(LogMockDataAlreadySeeded),
            Level = LogLevel.Information,
            Message = "Mock data already seeded for user {UserId}; skipping")]
        private static partial void LogMockDataAlreadySeeded(ILogger logger, string userId);

        /// <summary>
        /// Logs that no existing mock data was found, so nothing was removed.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userId"></param>
        [LoggerMessage(
            EventName = nameof(LogNoMockDataToRemove),
            Level = LogLevel.Information,
            Message = "No existing mock data found for user {UserId}; nothing to remove")]
        private static partial void LogNoMockDataToRemove(ILogger logger, string userId);

        /// <summary>
        /// Logs the completed mock data removal with its totals.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="accounts"></param>
        /// <param name="transactions"></param>
        /// <param name="ledgerEntries"></param>
        [LoggerMessage(
            EventName = nameof(LogMockDataRemoved),
            Level = LogLevel.Information,
            Message = "Removed existing mock data: {Accounts} accounts, {Transactions} transactions, {LedgerEntries} ledger entries")]
        private static partial void LogMockDataRemoved(ILogger logger, int accounts, int transactions, int ledgerEntries);

        /// <summary>
        /// Logs that the required institution was not found, so mock data cannot be seeded.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="institutionCode"></param>
        [LoggerMessage(
            EventName = nameof(LogMockInstitutionNotFound),
            Level = LogLevel.Warning,
            Message = "Institution {InstitutionCode} not found; cannot seed mock data")]
        private static partial void LogMockInstitutionNotFound(ILogger logger, string institutionCode);

        /// <summary>
        /// Logs the completed mock data seed with its totals.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="accounts"></param>
        /// <param name="transactions"></param>
        /// <param name="ledgerEntries"></param>
        [LoggerMessage(
            EventName = nameof(LogMockDataSeeded),
            Level = LogLevel.Information,
            Message = "Seeded mock data: {Accounts} accounts, {Transactions} transactions, {LedgerEntries} ledger entries")]
        private static partial void LogMockDataSeeded(ILogger logger, int accounts, int transactions, int ledgerEntries);
    }
}
