namespace BasicFinance.Api.Features.Insights;

/// <summary>
/// Shared balance calculations used by the insights endpoints.
/// </summary>
internal static class BalanceCalculations
{
    /// <summary>
    /// Calculates the percentage change in the size of a balance between the opening and closing balance,
    /// rounded to one decimal place.
    /// </summary>
    /// <remarks>
    /// Assets: positive when the balance grew (1,000 → 1,031 = 3.1).
    /// Liabilities are stored as negative values, so the change is measured on the amount owed: positive when
    /// the debt grew (−2,000 → −2,240 = 12.0), negative when it was paid down. The client decides whether a
    /// change is good or bad from the account type.
    /// Returns zero when the opening balance is zero, as there is no base to compare against.
    /// </remarks>
    /// <param name="openingBalance">The balance at the start of the period.</param>
    /// <param name="closingBalance">The latest balance.</param>
    /// <param name="isLiability">Whether the balance belongs to a liability account type.</param>
    /// <returns>The percentage change, e.g. <c>12.0</c> for a 12% increase.</returns>
    public static decimal CalculatePercentChange(decimal openingBalance, decimal closingBalance, bool isLiability)
    {
        if (openingBalance == 0)
        {
            return 0m;
        }

        var change = isLiability
            ? openingBalance - closingBalance
            : closingBalance - openingBalance;

        return Math.Round(change / Math.Abs(openingBalance) * 100, 1);
    }
}
