# Home Page Design

## Purpose

The Home page is the landing page and primary dashboard. It gives the user an at-a-glance view of their financial health across all accounts and institutions.

## Route

```typescript
{ path: '**', component: Home, canActivate: [authGuard], title: 'Home' }
```

Catch-all route — default destination after login.

## Layout

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

Donut chart showing balance distribution by account type.

- **Type**: Donut chart (ECharts)
- **Center**: Total net worth figure
- **Segments**: Checking, Savings, Investments (and Credit Cards if applicable)
- **Below chart**: Expandable list per account type showing individual accounts
  - Account name, institution, balance, % of type total
- **Color**: Each account type gets a consistent color

**Data source**: Same `AccountAnalyticsResponse` as Row 1 (reuses `currentPeriodBreakdown`).

### Row 3 — Recent Transactions

Simple list of the 5 most recent transactions across all accounts.

- **Format**: Compact list items (not a full table)
- **Columns shown**: Date, Description, Account, Amount
- **Amount coloring**: Green for income, Red for spend
- **CTA**: "View all transactions" link at bottom → navigates to `/Transactions`
- **Sort**: By date descending

**Data source**: `GET api/transactions?page=1&pageSize=5&sortField=Date&sortDirection=Descending`

Returns `ListResult<Transaction>`. Part of initial load gate.

## Period Selector Behavior

- Defaults to `Monthly`
- Affects all three data sources simultaneously
- No custom date range support (Transactions page handles that)

## Welcome Header

Above the cards, display a greeting:

- **Format**: `"Good Morning [FirstName]"` / `"Good Afternoon [FirstName]"`
- **Data source**: OAuth user profile loaded on init
- **Sub-text**: "View your financial overview"

## Component Structure

```
features/home/
├── home.ts                     # Main component
├── home.html
├── home.css
├── home-page-service.ts        # loading(), error(), computed data signals
└── components/
    ├── spend-activity-chart/
    ├── spend-activity-chart-skeleton/
    ├── account-net-worth-breakdown/
    ├── account-net-worth-breakdown-skeleton/
    ├── recent-transactions/
    └── recent-transactions-skeleton/
```

Shared components (live in `shared/ui/`):
- `SummaryCard` / `SummaryCardSkeleton`

## Data Flow

```
HomePageService
├── Injects: AccountClient, TransactionsClient, SpendingClient (from core/api/)
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

### Page Service

```typescript
@Injectable()
export class HomePageService {
  private readonly accountClient = inject(AccountClient);
  private readonly transactionsClient = inject(TransactionsClient);
  private readonly spendingClient = inject(SpendingClient);

  // Raw resources
  readonly balanceSummaryResource = httpResource<AccountAnalyticsResponse>(...);
  readonly spendingOverTimeResource = httpResource<SpendingOverTimeSummary>(...);
  readonly transactionsResource = httpResource<ListResult<Transaction>>(...);

  // Initial load gate — stays false once all resources have loaded
  readonly loading = computed(() =>
    !this.balanceSummaryResource.hasValue() ||
    !this.spendingOverTimeResource.hasValue() ||
    !this.transactionsResource.hasValue(),
  );

  // Initial load error — only fires if a resource errored before returning data
  readonly error = computed(() =>
    (this.balanceSummaryResource.error() !== null && !this.balanceSummaryResource.hasValue()) ||
    (this.spendingOverTimeResource.error() !== null && !this.spendingOverTimeResource.hasValue()) ||
    (this.transactionsResource.error() !== null && !this.transactionsResource.hasValue()),
  );

  // Transformed data signals
  readonly currentNetWorth = computed(
    () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.balance ?? 0,
  );
  readonly previousNetWorth = computed(
    () => this.balanceSummaryResource.value()?.previousPeriodBreakdown.balance ?? 0,
  );
  readonly currentChecking = computed(
    () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.accountTypeBreakdowns['CHK']?.balance ?? 0,
  );
  readonly previousChecking = computed(
    () => this.balanceSummaryResource.value()?.previousPeriodBreakdown.accountTypeBreakdowns['CHK']?.balance ?? 0,
  );
  // ... same pattern for Savings, Investments
  readonly currentPeriodBreakdown = computed(
    () => this.balanceSummaryResource.value()?.currentPeriodBreakdown ?? { balance: 0, accountTypeBreakdowns: {} },
  );
  readonly spendingOverTimeData = computed(
    () => this.spendingOverTimeResource.value() ?? null,
  );
  readonly recentTransactions = computed(
    () => this.transactionsResource.value()?.items ?? [],
  );
}
```

### Page Component

```typescript
@Component({ ... })
export class Home implements OnInit {
  private readonly pageService = inject(HomePageService);

  // Initial load gates
  readonly loading = this.pageService.loading;
  readonly error = this.pageService.error;

  // Data signals (for template binding)
  readonly currentNetWorth = this.pageService.currentNetWorth;
  readonly recentTransactions = this.pageService.recentTransactions;
  // ... etc
}
```

### Template

```html
@if (loading()) {
  <skeleton-layout />
} @else if (error()) {
  <error-state />
} @else {
  <!-- Row 1 -->
  <summary-card [value]="currentNetWorth()" [previous]="previousNetWorth()" />
  <!-- Row 2 -->
  <spend-activity-chart [data]="spendingOverTimeData()" />
  <account-net-worth-breakdown [breakdown]="currentPeriodBreakdown()" />
  <!-- Row 3 -->
  <recent-transactions [transactions]="recentTransactions()" />
}
```

All three resources fire in parallel on component init.

## Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` for Row 1, chart skeletons for Row 2, `RecentTransactionsSkeleton` for Row 3.
- **Error**: All components hidden. Generic error message with retry button centered on page.

### Subsequent Fetch (Period Change)

- **Error**: Current data remains visible. Toast notification appears: "Failed to refresh data. Click to retry."
- Toast auto-dismisses after 5 seconds. Clicking the toast re-triggers the failed resource.

### No Data

- **No data**: Cards show `$0.00` with no delta. Charts show a centered "No data for this period" message.
