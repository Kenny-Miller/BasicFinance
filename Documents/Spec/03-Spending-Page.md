# Spending Page Specification

## Status: Approved

## Purpose

The Spending page focuses on how money is being spent. It answers: "Where did my money go this period?" with category-level granularity and drill-down to individual transactions.

**Implementation status**: Requires additions. Current implementation has summary tile + donut chart. New design adds Row 1 summary cards, Row 2 trend chart, and Row 3 transaction table.

---

## 1. Route

```typescript
{ path: 'Spending', component: Spending, canActivate: [authGuard], title: 'Spending' }
```

---

## 2. Layout

Follows the 3-row shared layout pattern defined in `00-Architecture-Shared-Components.md`.

### Row 1 — Spending Summary Cards (3 cards)

| Card | Type | Current Value | Previous Value | Delta Meaning |
|---|---|---|---|---|
| **Total Spend** | `SummaryCard` | Sum of all spending transactions | Same for previous period | Red = more spending, Green = less |
| **Top Category** | `InfoCard` | Category with highest spend + amount | Same for previous period | N/A (non-numeric metric) |
| **Daily Average** | `SummaryCard` | Total spend ÷ days in period | Same for previous period | Red = higher avg, Green = lower avg |

**Data source**: `GET api/Spending/SpendingActivityByPeriod?startDate={}&spendingPeriod={}` (existing endpoint)

Returns `SpendingByPeriod`. Top category and daily average are computed client-side from the response.

**Note**: `InfoCard` is a new component variant for the Top Category card. It displays a text label (category name) + amount, not a pure currency delta. See `00-Architecture-Shared-Components.md §2.5` for InfoCard design.

### Row 2 — Charts

#### Left: Spending Trend Over Time

Area chart showing daily spending throughout the selected period.

- **Type**: Area chart (ECharts)
- **Current period**: Solid area fill, primary color
- **Previous period**: Dashed line overlay, muted color
- **X-axis**: Days in period
- **Y-axis**: Daily spend amount
- **Tooltip**: Date + daily spend amount
- **Header metrics**: Total spend for period + difference vs previous period

**Data source**: `GET api/Spending/SpendingOverTimeSummary` (existing endpoint)

Returns `SpendingOverTimeSummary` with `currentMonthActivity` and `previousMonthActivity`.

**Note**: This chart is currently missing from the Spending page. It exists on the Home page as `SpendActivityChart` and should be moved to `shared/ui/charts/` for reuse.

#### Right: Spending by Category Donut

Donut chart showing the breakdown of total spending by category.

- **Type**: Donut chart (ECharts) — uses shared `CategoryPieChart` configuration
- **Center**: Total spend figure
- **Segments**: Each spending category with consistent color mapping (`CATEGORY_COLORS`)
- **Legend**: Side legend with category name + amount + percentage
- **Interaction**: Clicking a segment highlights that category and filters the transaction table below

**Data source**: Same `SpendingByPeriod` response as Row 1 (reuses `spendingActivityByCategory`).

### Row 3 — Category Breakdown + Transaction Table

Two sections within one card:

#### Category Summary Table

A table summarizing spending per category. Clicking a row filters the transaction table below.

| Column | Description |
|---|---|
| **Category** | Category name (clickable to filter transactions) |
| **Amount** | Total spend in this category |
| **% of Total** | Percentage of total spending |
| **Transactions** | Count of transactions in this category |
| **Daily Avg** | Average daily spend in this category |

This table mirrors the donut chart data in tabular form.

#### Transaction Detail Table

Below the category summary, a paginated table of transactions:

- **Default**: Shows all spending transactions for the period
- **Filtered**: When a category is clicked (from donut or category table), filters to that category only
- **Columns**: Date, Description, Account, Amount
- **Pagination**: Traditional page numbers, 20 items per page
- **Clear filter**: "Show all" button to reset category filter
- **Initial load**: Page 1 data is part of the initial load gate (`loading()`)
- **Page turns / category filter**: Show a local loading indicator on the table only (`resource.isLoading()`)

**Data source**: `GET api/transactions?page={}&pageSize={}&sortField=Date&sortDirection=Descending&transactionCategoryCode={}&transactionTypeCode=Debit`

Returns `ListResult<Transaction>`. Note: pre-filtered to Debit (spend) transactions only.

---

## 3. Component Structure

```
features/spending/
├── spending.ts                        # Main component
├── spending.html
├── spending.css                       # Empty (Tailwind utility classes)
├── spending-page-service.ts           # loading(), error(), computed data signals
└── components/
    └── category-breakdown-table/      # Category summary table (feature-specific)
        category-breakdown-table-skeleton/
```

Shared components used:
- `SummaryCard` / `SummaryCardSkeleton` — Row 1 cards (Total Spend, Daily Average)
- `InfoCard` / `InfoCardSkeleton` — Row 1 card (Top Category)
- `PeriodSelector` — Page header
- `SpendActivityChart` / `SpendActivityChartSkeleton` — Row 2 left (moved from Home)
- `CategoryPieChart` / `CategoryPieChartSkeleton` — Row 2 right
- `TransactionTable` / `TransactionTableSkeleton` — Row 3 transaction detail
- `PaginationControls` — Row 3 pagination

---

## 4. Data Flow

```
SpendingPageService
├── Injects: SpendingClient, TransactionsClient
│
├── loading()  → true while any of 3 resources has not returned a value
├── error()    → true if any resource failed before returning a value
│
├── Computed data signals:
│   ├── spendingByPeriod      → SpendingByPeriod (Row 1 + Row 2 right + Row 3 category table)
│   ├── spendingOverTime      → SpendingOverTimeSummary (Row 2 left)
│   ├── transactions          → ListResult<Transaction> (Row 3 page 1)
│   ├── totalSpend            → number (derived from spendingByPeriod)
│   ├── topCategory           → SpendingActivity | null (derived from spendingByPeriod)
│   └── dailyAverage          → number (derived from totalSpend + days in period)
│
└── Exposed resources:
    ├── spendingResource
    ├── spendingOverTimeResource
    └── transactionsResource  → table checks this for page-turn loading
```

`spendingResource` is the primary data source — the over-time resource is supplementary for the trend chart. The transactions resource is part of initial load but exposed for page-turn pagination.

---

## 5. API Changes Required

### 5.1 Backend Changes

No new endpoints required. All data comes from existing endpoints:

| Data | Endpoint |
|---|---|
| Row 1 cards, Row 2 right, Row 3 category table | `GET api/Spending/SpendingActivityByPeriod` |
| Row 2 left chart | `GET api/Spending/SpendingOverTimeSummary` |
| Row 3 transaction table | `GET api/transactions` (with `transactionTypeCode=Debit` filter) |

### 5.2 Client Changes

`SpendingClient` needs a new `httpResource` factory for the over-time summary endpoint (currently not wired to the Spending page):

```typescript
getSpendingOverTime(period: signal<TimePeriod>): HttpResource<SpendingOverTimeSummary>
```

`TransactionsClient` needs to support category filter for the transaction table:

```typescript
getTransactions(params: {
  page: signal<number>,
  pageSize: signal<number>,
  period: signal<TimePeriod>,
  categoryCode?: signal<string | null>,
}): HttpResource<ListResult<Transaction>>
```

### 5.3 Previous Period Data

The `SpendingActivityByPeriod` endpoint currently returns data for a single period. For the summary cards (Total Spend, Daily Average), previous period data is needed for delta calculation.

**Option A**: Extend the endpoint to return both periods (like `balanceSummary` does with `currentPeriodBreakdown` / `previousPeriodBreakdown`).

**Option B**: Make two parallel calls (current + previous period) and merge client-side.

**Recommendation**: Option A. The `SpendingByPeriod` response should be extended to include a `previousPeriod` field with the same structure. This is consistent with the `AccountAnalyticsResponse` pattern.

---

## 6. Period Selector Behavior

- Defaults to `Monthly`
- Affects all data sources simultaneously
- No custom date range (Transactions page handles custom ranges)

---

## 7. Design Changes from Current

| Current | New |
|---|---|
| Single `SpendingSummaryTile` | 3 `SummaryCard` / `InfoCard` components |
| No trend chart in Row 2 | Spending trend area chart (left) |
| Donut chart (right) — exists | Enhanced with click-to-filter interaction |
| No transaction table | Paginated transaction table (Row 3) |
| No category summary table | Category breakdown table with click-to-filter |
| PeriodSelector feature-local | Moved to `shared/ui/` |

---

## 8. Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` ×3 for Row 1, chart skeletons for Row 2, `TransactionTableSkeleton` for Row 3.
- **Error**: All components hidden. `ErrorState` with retry button centered on page.

### Subsequent Fetch (Period Change / Category Filter)

- **Error on Row 1 or Row 2 resources**: Current data remains visible. Toast: "Failed to refresh data. Click to retry."
- **Error on Row 3 (table fetch)**: Table shows inline error with retry button. Rest of page unchanged.

### No Data

- **No spending data**: Cards show `$0.00`. Donut shows "No spending data". Table shows empty message.
- **Single category**: Donut shows one segment. Table shows all transactions for that category.

---

## 9. Future-Proofing Notes

- **Budget integration**: Future enhancement — show budget vs actual per category. The category breakdown table should have a reserved column slot for "Budget" and "Variance" that can be enabled when budget data is available.
- **Spending alerts**: Future enhancement — notify when a category exceeds a threshold. The page service's `topCategory` and `totalSpend` signals are the foundation for alert logic.
- **Category merging**: Future enhancement — allow user to merge categories. The category breakdown data should support user-defined category groupings.
