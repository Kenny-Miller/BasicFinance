# Spending Page Design

## Purpose

The Spending page focuses on how money is being spent. It answers: "Where did my money go this period?" with category-level granularity and drill-down to individual transactions.

## Route

```typescript
{ path: 'Spending', component: Spending, canActivate: [authGuard], title: 'Spending' }
```

## Layout

### Row 1 — Spending Summary Cards (3 cards)

| Card | Current Value | Previous Value | Delta Meaning |
|---|---|---|---|
| **Total Spend** | Sum of all spending transactions | Same for previous period | Red = more spending, Green = less |
| **Top Category** | Category with highest spend | Same for previous period | Shows category name + amount |
| **Daily Average** | Total spend divided by days in period | Same for previous period | Red = higher avg, Green = lower avg |

**Data source**: `GET api/Spending/SpendingActivityByPeriod?startDate={}&spendingPeriod={}`

Returns `SpendingByPeriod`. Top category and daily average are computed client-side from the response.

**Note**: The `SummaryCard` component needs a variant for non-currency metrics (Top Category shows text + amount, not just a dollar figure). A new `InfoCard` component may be needed, or `SummaryCard` is extended with an optional `subLabel` input.

### Row 2 — Charts

#### Left: Spending Trend Over Time

Line chart showing daily spending throughout the selected period.

- **Type**: Area chart (ECharts)
- **Current period**: Solid area fill, primary color
- **Previous period**: Dashed line overlay, muted color
- **X-axis**: Days in period
- **Y-axis**: Daily spend amount
- **Tooltip**: Date + daily spend amount
- **Header metrics**: Total spend for period + difference vs previous period

**Data source**: `GET api/Spending/SpendingOverTimeSummary`

Returns `SpendingOverTimeSummary` with `currentMonthActivity` and `previousMonthActivity`.

#### Right: Spending by Category Donut

Donut chart showing the breakdown of total spending by category.

- **Type**: Donut chart (ECharts)
- **Center**: Total spend figure
- **Segments**: Each spending category (24 possible categories from `CATEGORY_CODE_TO_NAME`)
- **Legend**: Side legend with category name + amount + percentage
- **Interaction**: Clicking a segment highlights that category and filters the category breakdown table below

**Data source**: Same `SpendingByPeriod` response as Row 1 (reuses `spendingActivityByCategory`).

### Row 3 — Category Breakdown Table

A table summarizing spending per category, followed by a paginated transaction table for the selected category.

#### Category Summary Section

| Column | Description |
|---|---|
| **Category** | Category name (clickable to filter transactions) |
| **Amount** | Total spend in this category |
| **% of Total** | Percentage of total spending |
| **Transactions** | Count of transactions in this category |
| **Daily Avg** | Average daily spend in this category |

This table mirrors the donut chart data in tabular form. Clicking a row filters the transaction table below to that category.

#### Transaction Detail Section

Below the category summary, a paginated table of transactions:

- **Default**: Shows all transactions for the period
- **Filtered**: When a category is clicked, filters to that category only
- **Columns**: Date, Description, Account, Amount
- **Pagination**: Traditional page numbers, 20 items per page
- **Clear filter**: "Show all" button to reset category filter
- **Initial load**: Page 1 data is part of the initial load gate (`loading()`)
- **Page turns / category filter**: Show a local loading indicator on the table only (`resource.isLoading()`)

**Data source**: `GET api/transactions?page={}&pageSize={}&sortField=Date&sortDirection=Descending&transactionCategoryId={}`

## Period Selector Behavior

- Defaults to `Monthly`
- Affects all data sources simultaneously
- No custom date range (Transactions page handles custom ranges)

## Component Structure

```
features/spending/
├── spending.ts                        # Main component
├── spending.html
├── spending.css
├── spending-page-service.ts           # loading(), error(), computed data signals
└── components/
    ├── spending-summary-tile/         (existing, needs redesign to Row 1 cards)
    ├── spending-summary-tile-skeleton/
    ├── category-pie-chart/            (existing, already a donut)
    ├── category-pie-chart-skeleton/
    ├── category-breakdown-list/       (existing, already tabular)
    ├── category-breakdown-list-skeleton/
    ├── spending-trend-chart/          (new, for Row 2 left)
    ├── spending-trend-chart-skeleton/ (new)
    └── spending-transactions-table/   (new, for Row 3)
        spending-transactions-table-skeleton/ (new)
```

## Data Flow

```
SpendingPageService
├── Injects: SpendingClient, TransactionsClient (from core/api/)
│
├── loading()  → true while any of the 3 resources has not returned a value
├── error()    → true if any resource failed before returning a value
│
├── Computed data signals:
│   ├── spendingByPeriod      → SpendingByPeriod (Row 1 + Row 2 right + Row 3 category table)
│   ├── spendingOverTime      → SpendingOverTimeSummary (Row 2 left)
│   └── transactions          → ListResult<Transaction> (Row 3 page 1)
│
└── Exposed resources:
    ├── spendingResource
    ├── spendingOverTimeResource
    └── transactionsResource  → table checks this for page-turn loading
```

### Page Service

```typescript
@Injectable()
export class SpendingPageService {
  private readonly spendingClient = inject(SpendingClient);
  private readonly transactionsClient = inject(TransactionsClient);

  // Raw resources — all 3 are part of initial load
  readonly spendingResource = httpResource<SpendingByPeriod>(...);
  readonly spendingOverTimeResource = httpResource<SpendingOverTimeSummary>(...);
  readonly transactionsResource = httpResource<ListResult<Transaction>>(...);

  // Initial load gate — includes transactions page 1
  readonly loading = computed(() =>
    !this.spendingResource.hasValue() ||
    !this.spendingOverTimeResource.hasValue() ||
    !this.transactionsResource.hasValue(),
  );

  // Initial load error
  readonly error = computed(() =>
    (this.spendingResource.error() !== null && !this.spendingResource.hasValue()) ||
    (this.spendingOverTimeResource.error() !== null && !this.spendingOverTimeResource.hasValue()) ||
    (this.transactionsResource.error() !== null && !this.transactionsResource.hasValue()),
  );

  // Transformed data signals
  readonly spendingByPeriod = computed(() => this.spendingResource.value() ?? null);
  readonly spendingOverTime = computed(() => this.spendingOverTimeResource.value() ?? null);
  readonly transactions = computed(() => this.transactionsResource.value() ?? null);

  // Derived signals for Row 1 cards
  readonly totalSpend = computed(() => {
    const data = this.spendingByPeriod();
    return data?.spendingActivityByCategory.reduce((sum, c) => sum + c.amount, 0) ?? 0;
  });
  readonly topCategory = computed(() => {
    const data = this.spendingByPeriod();
    return data?.spendingActivityByCategory.toSorted((a, b) => b.amount - a.amount)[0] ?? null;
  });
  readonly dailyAverage = computed(() => {
    const spend = this.totalSpend();
    const days = /* days in period */;
    return days ? spend / days : 0;
  });
}
```

`spendingResource` is the primary data source — the over-time resource is supplementary for the trend chart. The transactions resource is part of initial load but exposed for page-turn pagination.

## Design Changes from Current

1. **Row 1**: Replace single `SpendingSummaryTile` with 3 `SummaryCard` components for consistency with other pages.
2. **Row 2 left**: Add spending trend chart (currently missing — current layout has summary tile + donut side by side).
3. **Row 2 right**: Keep existing donut chart, enhance with click-to-filter interaction.
4. **Row 3**: Add paginated transaction table (currently missing — current layout ends at the category breakdown list).

## Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` for Row 1, chart skeletons for Row 2, `SpendingTransactionsTableSkeleton` for Row 3.
- **Error**: All components hidden. Generic error message with retry button centered on page.

### Subsequent Fetch (Period Change / Category Filter)

- **Error on Row 1 or Row 2 resources**: Current data remains visible. Toast notification appears: "Failed to refresh data. Click to retry."
- **Error on Row 3 (table fetch)**: Table shows inline error with retry button. Rest of page unchanged.

### No Data

- **No spending data**: Cards show `$0.00`. Donut shows "No spending data". Table shows empty message.
- **Single category**: Donut shows one segment. Table shows all transactions for that category.
