# Home Page Specification

## Status: Approved

## Purpose

The Home page is the landing page and primary dashboard. It gives the user an at-a-glance view of their financial health across all accounts and institutions.

**Implementation status**: Fully implemented. This spec documents the reference pattern that other pages follow.

---

## 1. Route

```typescript
{ path: '**', component: Home, canActivate: [authGuard], title: 'Home' }
```

Catch-all route — default destination after login.

---

## 2. Layout

Follows the 3-row shared layout pattern defined in `00-Architecture-Shared-Components.md`.

### Row 1 — Net Worth Summary Cards (4 cards)

| Card | Current Value | Previous Value | Delta Meaning |
|---|---|---|---|
| **Net Worth** | Total balance across all accounts | Same for previous period | Green = growth, Red = decline |
| **Checking** | Sum of all `CHK` account balances | Same for previous period | Green = growth, Red = decline |
| **Savings** | Sum of all `SAV` account balances | Same for previous period | Green = growth, Red = decline |
| **Investments** | Sum of all `INV` account balances | Same for previous period | Green = growth, Red = decline |

**Data source**: `GET api/accounts/balanceSummary?TimePeriod={period}`

Returns `AccountAnalyticsResponse` with both `currentPeriodBreakdown` and `previousPeriodBreakdown`.

### Row 2 — Charts

#### Left: Spending Activity Chart

Line chart comparing daily spending of current period vs previous period.

- **Type**: Dual line chart (ECharts)
- **Current period**: Solid line, primary color
- **Previous period**: Dashed line, muted color with gradient fill
- **X-axis**: Days in period (hidden labels, tooltip on hover)
- **Y-axis**: Daily spend amount (hidden axis, values in tooltip)
- **Header metrics**: Total monthly spend + spend difference vs previous period
- **Color logic**: If spend increased → red, if decreased → green

**Data source**: `GET api/Spending/SpendingOverTimeSummary`

Returns `SpendingOverTimeSummary` with `currentMonthActivity` and `previousMonthActivity` arrays.

#### Right: Net Worth Breakdown

Balance distribution by account type shown as a list with progress bars.

- **Segments**: Checking, Savings, Investments (and Credit Cards if applicable)
- **Below chart**: Per account type showing individual accounts
  - Account name, institution, balance, % of type total
- **Color**: Each account type gets a consistent color

**Data source**: Same `AccountAnalyticsResponse` as Row 1 (reuses `currentPeriodBreakdown`).

### Row 3 — Recent Transactions

Compact list of the 5 most recent transactions across all accounts.

- **Format**: List items (not a full table)
- **Columns shown**: Date, Description, Account, Amount
- **Amount coloring**: Green for income, Red for spend
- **CTA**: "View all transactions" link → navigates to `/Transactions`
- **Sort**: By date descending

**Data source**: `GET api/transactions?page=1&pageSize=5&sortField=Date&sortDirection=Descending`

Returns `ListResult<Transaction>`. Part of initial load gate.

---

## 3. Component Structure

```
features/home/
├── home.ts                           # Main component
├── home.html
├── home.css                          # Empty (Tailwind utility classes)
├── home-page-service.ts              # loading(), error(), computed data signals
└── components/
    ├── spend-activity-chart/
    ├── spend-activity-chart-skeleton/
    ├── account-net-worth-breakdown/
    ├── account-net-worth-breakdown-skeleton/
    ├── recent-transactions/
    └── recent-transactions-skeleton/
```

Shared components used (from `shared/ui/`):
- `SummaryCard` / `SummaryCardSkeleton` — Row 1 cards

---

## 4. Data Flow

```
HomePageService
├── Injects: HomeClient (features/home/data/home-client.ts)
│
├── loading()  → true while any resource has not returned a value
├── error()    → true if any resource failed before returning a value
│
├── Computed data signals:
│   ├── currentNetWorth
│   ├── previousNetWorth
│   ├── netWorthDelta
│   ├── currentChecking / previousChecking
│   ├── currentSavings / previousSavings
│   ├── currentInvestments / previousInvestments
│   ├── currentPeriodBreakdown
│   ├── spendingOverTimeData
│   └── recentTransactions
│
└── Exposed resources:
    ├── balanceSummaryResource
    ├── spendingOverTimeResource
    └── transactionsResource
```

All three resources fire in parallel on component init.

---

## 5. Welcome Header

Above the cards, display a greeting:

- **Format**: `"Good Morning [FirstName]"` / `"Good Afternoon [FirstName]"`
- **Data source**: OAuth user profile loaded on init
- **Sub-text**: "View your financial overview"

---

## 6. Period Selector Behavior

- Defaults to `Monthly`
- Affects all three data sources simultaneously
- No custom date range support (Transactions page handles that)

---

## 7. Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` ×4 for Row 1, chart skeletons for Row 2, `RecentTransactionsSkeleton` for Row 3.
- **Error**: All components hidden. Generic error message with retry button centered on page.

### Subsequent Fetch (Period Change)

- **Error**: Current data remains visible. Toast notification appears: "Failed to refresh data. Click to retry."
- Toast auto-dismisses after 5 seconds. Clicking the toast re-triggers the failed resource.

### No Data

- **No data**: Cards show `$0.00` with no delta. Charts show a centered "No data for this period" message.

---

## 8. Items to Move to Shared

The following components are currently feature-local but used by other pages and should be moved to `shared/ui/`:

| Component | Current Path | Target Path |
|---|---|---|
| `SpendActivityChart` | `features/home/components/spend-activity-chart/` | `shared/ui/charts/spend-activity-chart/` |
| `SpendActivityChartSkeleton` | `features/home/components/spend-activity-chart-skeleton/` | `shared/ui/charts/spend-activity-chart-skeleton/` |
| `RecentTransactions` | `features/home/components/recent-transactions/` | `shared/ui/transactions/recent-transactions/` |

---

## 9. Future-Proofing Notes

- **OpenAPI codegen**: The `HomeClient` is structured as a thin wrapper. When OpenAPI codegen is introduced, the client can be replaced with generated types while keeping the page service pattern intact.
- **Configurable app settings**: Account type labels and colors are currently from static maps. When a runtime config endpoint is available, the Home page should consume `AccountType` config from the config signal.
- **SummaryCard period label**: The card's delta label ("vs last month") must become dynamic based on the active period. This applies to all pages.
