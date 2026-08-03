# Transactions Page Design

## Purpose

The Transactions page is the primary tool for browsing, filtering, and analyzing individual transactions. Users can narrow down to specific time ranges, amounts, categories, and types.

## Route

```typescript
{ path: 'Transactions', component: Transactions, canActivate: [authGuard], title: 'Transactions' }
```

## Layout

### Row 1 — Transaction Summary Cards (4 cards)

| Card | Current Value | Previous Value | Delta Meaning |
|---|---|---|---|
| **Total Transactions** | Count of all transactions in period | Same for previous period | Green = more activity, Red = less |
| **Total Spend** | Sum of all negative transactions | Same for previous period | Red = more spending, Green = less |
| **Total Income** | Sum of all positive transactions | Same for previous period | Green = more income, Red = less |
| **Net Flow** | Income minus spend | Same for previous period | Green = positive flow, Red = negative |

**Data source**: `GET api/transactions/summary?TimePeriod={period}&startDate={}&endDate={}`

Returns a new response type:

```typescript
interface TransactionSummaryResponse {
  currentPeriod: TransactionPeriodSummary;
  previousPeriod: TransactionPeriodSummary;
}

interface TransactionPeriodSummary {
  totalCount: number;
  totalSpend: number;
  totalIncome: number;
  netFlow: number;
}
```

### Row 2 — Charts

#### Left: Spending Over Time with Transaction Count

Dual-axis chart showing daily spending trend overlaid with transaction count markers.

- **Type**: Dual-axis ECharts (line + scatter/bubble)
- **Primary Y-axis** (left): Daily spend amount as area chart (solid fill, primary color)
- **Secondary Y-axis** (right): Transaction count as bubble markers (size proportional to count)
- **X-axis**: Days in period
- **Tooltip**: Shows both spend amount and transaction count for hovered day
- **Previous period**: Optional dashed overlay (toggleable)

**Data source**: `GET api/transactions/dailySummary?TimePeriod={period}&startDate={}&endDate={}`

Returns:

```typescript
interface DailyTransactionSummary {
  date: string;
  totalSpend: number;
  transactionCount: number;
}

interface DailySummaryResponse {
  currentPeriod: DailyTransactionSummary[];
  previousPeriod: DailyTransactionSummary[];
}
```

#### Right: Spending by Category Breakdown

Donut chart showing how spending is distributed across categories for the selected period.

- **Type**: Donut chart (ECharts)
- **Center**: Total spend figure
- **Segments**: Each spending category with consistent color mapping
- **Below chart**: Compact table of top N categories (name, amount, % of total, transaction count)
- **Drill-down**: Clicking a category segment filters the main transaction table below

**Data source**: Reuses `GET api/Spending/SpendingActivityByPeriod?startDate={}&spendingPeriod={}`

Returns `SpendingByPeriod` with `spendingActivityByCategory`.

### Row 3 — Transaction Table

Full-featured, paginated transaction table with filtering and sorting.

#### Filter Bar

Persistent filter bar at the top of the table section:

| Filter | Type | Description |
|---|---|---|
| **Date Range** | Date pickers (start/end) | Overrides period selector for granular filtering |
| **Amount Range** | Number inputs (min/max) | Filter by transaction amount |
| **Transaction Type** | Dropdown | Income, Spend, Transfer |
| **Category** | Dropdown | All spending categories |
| **Search** | Text input | Free-text search on description field |
| **Account** | Dropdown | Filter by account name (new) |

Filters sync to URL query params for shareable links.

#### Table Columns

| Column | Sortable | Description |
|---|---|---|
| **Date** | Yes (default, desc) | Transaction date |
| **Description** | Yes | Merchant or transaction description |
| **Category** | Yes | Transaction category name |
| **Account** | Yes | Account name |
| **Type** | Yes | Income/Spend/Transfer |
| **Amount** | Yes | Formatted currency, color-coded (green=income, red=spend) |

#### Pagination

- **Style**: Traditional page numbers (not infinite scroll)
- **Default page size**: 20 items
- **Page size options**: 10, 20, 50, 100
- **Controls**: Previous/Next buttons, page number buttons, jump to page input
- **Info text**: "Showing X-Y of Z transactions"
- **Initial load**: Page 1 data is part of the initial load gate (`loading()`)
- **Page turns**: Show a local loading indicator on the table only (`resource.isLoading()`)

**Data source**: `GET api/transactions?page={}&pageSize={}&sortField={}&sortDirection={}&startDate={}&endDate={}&...`

Returns `ListResult<Transaction>`.

## Period Selector Behavior

- Defaults to `Monthly`
- **Extended mode**: Custom start/end date pickers override the period selector
- When custom dates are set, period selector shows "Custom Range"
- Clearing custom dates reverts to period selector value

## Component Structure

```
features/transactions/
├── transactions.ts                     # Main component
├── transactions.html
├── transactions.css
├── transactions-page-service.ts        # loading(), error(), computed data signals
└── components/
    ├── filter-bar/
    ├── transaction-summary-cards/      (new)
    ├── transaction-summary-skeleton/   (new)
    ├── daily-spend-chart/              (new)
    ├── daily-spend-chart-skeleton/     (new)
    ├── category-breakdown-chart/       (new)
    ├── category-breakdown-skeleton/    (new)
    └── transaction-table/              (new, replaces card-based list)
        transaction-table-skeleton/     (new)
```

## Data Flow

```
TransactionsPageService
├── Injects: TransactionsClient, SpendingClient (from core/api/)
│
├── loading()  → true while any of the 4 resources has not returned a value
├── error()    → true if any resource failed before returning a value
│
├── Computed data signals:
│   ├── summaryData         → TransactionSummaryResponse
│   ├── dailySummaryData    → DailySummaryResponse
│   ├── categoryBreakdown   → SpendingByPeriod
│   └── transactions        → ListResult<Transaction> (page 1)
│
└── Exposed resources:
    ├── summaryResource
    ├── dailySummaryResource
    ├── categoryBreakdownResource
    └── transactionsResource  → table checks this for page-turn loading
```

### Page Service

```typescript
@Injectable()
export class TransactionsPageService {
  private readonly transactionsClient = inject(TransactionsClient);
  private readonly spendingClient = inject(SpendingClient);

  // Raw resources — all 4 are part of initial load
  readonly summaryResource = httpResource<TransactionSummaryResponse>(...);
  readonly dailySummaryResource = httpResource<DailySummaryResponse>(...);
  readonly categoryBreakdownResource = httpResource<SpendingByPeriod>(...);
  readonly transactionsResource = httpResource<ListResult<Transaction>>(...);

  // Initial load gate — includes transactions page 1
  readonly loading = computed(() =>
    !this.summaryResource.hasValue() ||
    !this.dailySummaryResource.hasValue() ||
    !this.categoryBreakdownResource.hasValue() ||
    !this.transactionsResource.hasValue(),
  );

  // Initial load error
  readonly error = computed(() =>
    (this.summaryResource.error() !== null && !this.summaryResource.hasValue()) ||
    (this.dailySummaryResource.error() !== null && !this.dailySummaryResource.hasValue()) ||
    (this.categoryBreakdownResource.error() !== null && !this.categoryBreakdownResource.hasValue()) ||
    (this.transactionsResource.error() !== null && !this.transactionsResource.hasValue()),
  );

  // Transformed data signals
  readonly summaryData = computed(() => this.summaryResource.value() ?? null);
  readonly dailySummaryData = computed(() => this.dailySummaryResource.value() ?? null);
  readonly categoryBreakdown = computed(() => this.categoryBreakdownResource.value() ?? null);
  readonly transactions = computed(() => this.transactionsResource.value() ?? null);
}
```

All four resources fire on component init. The transaction table resource supports pagination — page turns show `resource.isLoading()` on the table only.

## Migration Notes

The current implementation uses a card-based infinite scroll list. The new design replaces this with:
1. Summary cards in Row 1 (new feature)
2. Charts in Row 2 (new feature)
3. A proper paginated table in Row 3 (replaces `TransactionCard` list)

The `FilterBar` component is reused and enhanced. The `transaction-card` component may be deprecated in favor of table rows.

## Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` for Row 1, chart skeletons for Row 2, `TransactionTableSkeleton` for Row 3.
- **Error**: All components hidden. Generic error message with retry button centered on page.

### Subsequent Fetch (Period Change / Filter Change)

- **Error on Row 1 or Row 2 resources**: Current data remains visible. Toast notification appears: "Failed to refresh data. Click to retry."
- **Error on Row 3 (table fetch)**: Table shows inline error with retry button. Rest of page unchanged.

### No Data

- **No transactions for period**: Cards show `$0.00` / `0 count`. Charts show "No data". Table shows centered empty message.
- **Filter yields no results**: Table shows "No transactions match your filters" with a "Clear filters" button.
