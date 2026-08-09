using BasicFinance.Infrastructure;
using BasicFinance.Infrastructure.Entities;

namespace BasicFinance.Api.IntegrationTests.Helpers;

public static class DbSeedHelper
{
    private static readonly Guid TestUserGoogleSpreadsheetId = new("00000000-0000-0000-0000-000000000001");

    public static async Task SeedGlobalDataAsync(AppDbContext dbContext, string userId, CancellationToken cancellationToken = default)
    {
        dbContext.UserGoogleSpreadsheets.Add(new(userId, "test-sheet-id", "Test Spreadsheet")
        {
            UserGoogleSpreadsheetId = TestUserGoogleSpreadsheetId,
        });

        dbContext.TransactionTypes.AddRange(
            new("CR", "Credit"),
            new("DR", "Debit"));

        dbContext.Institutions.AddRange(
            new("WF", "Wells Fargo", null),
            new("CHASE", "Chase", null),
            new("SCHW", "Charles Schwab", null));

        dbContext.AccountTypes.AddRange(
            new("CHK", "Checking"),
            new("SAV", "Savings"),
            new("CC", "Credit Card"),
            new("INV", "Investment"));

        dbContext.TransactionCategories.AddRange(
            new("CR", "Credit"),
            new("DR", "Debit"),
            new("UNC", "Uncategorized"),
            new("AUTO", "Auto and Transport"),
            new("BILLS", "Bills and Utilities"),
            new("BUSINESS", "Business"),
            new("CASH", "Cash & Checks"),
            new("DONATIONS", "Charitable Donations"),
            new("DINING", "Dining & Drinks"),
            new("EDUCATION", "Education"),
            new("ENTERTAINMENT", "Entertainment & Rec"),
            new("FAMILY", "Family Care"),
            new("FEES", "Fees"),
            new("GIFTS", "Gifts"),
            new("GROCERIES", "Groceries"),
            new("HEALTH", "Health & Wellness"),
            new("HOME", "Home & Garden"),
            new("LEGAL", "Legal"),
            new("LOAN", "Loan Payment"),
            new("MEDICAL", "Medical"),
            new("PERSONAL", "Personal Care"),
            new("PETS", "Pets"),
            new("SHOPPING", "Shopping"),
            new("SOFTWARE", "Software & Tech"),
            new("TAXES", "Taxes"),
            new("TRAVEL", "Travel & Vacation"),
            new("INCOME", "Income"),
            new("INVESTMENT", "Investment"),
            new("CREDIT", "Credit Card Payment"),
            new("IGNORE", "Ignore"),
            new("TRANSFER", "Internal Transfer"),
            new("REIMBURSEMENT", "Reimbursement"),
            new("SAVINGS", "Savings Transfer"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
