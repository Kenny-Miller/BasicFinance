# Transactions Page Specification

## Status: Approved

## Purpose

The Transactions page is the primary tool for browsing, filtering, and analyzing individual transactions. Users can narrow down to specific time ranges, amounts, categories, and types.

**Implementation status**: Requires significant changes. Current implementation uses infinite scroll with card-based list. New design adds summary cards (Row 1), charts (Row 2), and replaces the list with a paginated table (Row 3).

---

## 1. Route

```typescript
{ path: 'Transactions', component: Transactions, canActivate: [authGuard], title: 'Transactions' }
```

---

## 2. Layout

Follows the 3-row shared layout pattern defined in `00-Architecture-Shared-Components.md`.

### Row 1 — Transaction Summary Cards (4 cards)

| Card | Current Value | Previous Value | Delta Meaning |
|---|---|---|---|
| **Total Transactions** | Count of all transactions in period | Same for previous period | Green = more activity, Red = less |
| **Total Spend** | Sum of all negative (Debit) transactions | Same for previous period | Red = more spending, Green = less |
| **Total Income** | Sum of all positive (Credit) transactions | Same for previous period | Green = more income, Red = less |
| **Net Flow** | Income minus spend | Same for previous period | Green = positive flow, Red = negative |

**Data source**: `GET api/transactions/summary?TimePeriod={period}` (NEW endpoint)

Returns:

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

**Note**: `Total Transactions` card uses `InfoCard` variant (non-currency metric — shows count, not dollar amount). The other three cards use standard `SummaryCard`.

### Row 2 — Charts

#### Left: Spending Over Time with Transaction Count

Dual-axis chart showing daily spending trend overlaid with transaction count markers.

- **Type**: Dual-axis ECharts (area + bubble markers)
- **Primary Y-axis** (left): Daily spend amount as area chart (solid fill, primary color)
- **Secondary Y-axis** (right): Transaction count as bubble markers (size proportional to count)
- **X-axis**: Days in period
- **Tooltip**: Shows both spend amount and transaction count for hovered day
- **Previous period**: Optional dashed overlay (toggleable via checkbox in card header)

**Data source**: `GET api/transactions/dailySummary?TimePeriod={period}` (NEW endpoint)

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

- **Type**: Donut chart (ECharts) — uses shared `CategoryPieChart` configuration
- **Center**: Total spend figure
- **Segments**: Each spending category with consistent color mapping (`CATEGORY_COLORS`)
- **Below chart**: Compact table of top N categories (name, amount, % of total, transaction count)
- **Drill-down**: Clicking a category segment filters the main transaction table below

**Data source**: `GET api/Spending/SpendingActivityByPeriod?startDate={}&spendingPeriod={}` (existing endpoint)

Returns `SpendingByPeriod` with `spendingActivityByCategory`.

### Row 3 — Transaction Table

Full-featured, paginated transaction table with filtering and sorting.

#### Filter Bar

Persistent filter bar at the top of the table section. Uses shared `FilterBar` component (moved from `features/transactions/` to `shared/ui/filters/`).

| Filter | Type | Description |
|---|---|---|
| **Date Range** | Date pickers (start/end) | Overrides period selector for granular filtering |
| **Amount Range** | Number inputs (min/max) | Filter by transaction amount |
| **Transaction Type** | Dropdown | Income (Credit), Spend (Debit) |
| **Category** | Dropdown | All spending categories |
| **Search** | Text input | Free-text search on description field |

**Note**: Filters are NOT synced to URL query params.

#### Table Columns

| Column | Sortable | Description |
|---|---|---|
| **Date** | Yes (default, desc) | Transaction date |
| **Description** | Yes | Merchant or transaction description |
| **Category** | Yes | Transaction category name |
| **Account** | Yes | Account name |
| **Type** | Yes | Income/Spend |
| **Amount** | Yes | Formatted currency, color-coded (green=income, red=spend) |

#### Pagination

- **Style**: Traditional page numbers (replaces infinite scroll)
- **Default page size**: 20 items
- **Page size options**: 10, 20, 50, 100
- **Controls**: Previous/Next buttons, page number buttons, jump to page input
- **Info text**: "Showing X-Y of Z transactions"
- **Initial load**: Page 1 data is part of the initial load gate (`loading()`)
- **Page turns**: Show a local loading indicator on the table only (`resource.isLoading()`)

**Data source**: `GET api/transactions?page={}&pageSize={}&sortField={}&sortDirection={}&startDate={}&endDate={}&...` (existing endpoint, extended with new query params)

Returns `ListResult<Transaction>`.

---

## 3. Component Structure

```
features/transactions/
├── transactions.ts                     # Main component
├── transactions.html
├── transactions.css                    # Empty (Tailwind utility classes)
├── transactions-page-service.ts        # loading(), error(), computed data signals
└── components/
    └── (empty — all components are shared)
```

Shared components used:
- `SummaryCard` / `SummaryCardSkeleton` — Row 1 cards (Total Spend, Total Income, Net Flow)
- `InfoCard` / `InfoCardSkeleton` — Row 1 card (Total Transactions)
- `PeriodSelector` — Page header
- `DailySpendChart` / `DailySpendChartSkeleton` — Row 2 left (NEW)
- `CategoryPieChart` / `CategoryPieChartSkeleton` — Row 2 right
- `FilterBar` — Row 3 filter bar
- `TransactionTable` / `TransactionTableSkeleton` — Row 3 table (NEW, replaces card-based list)

---

## 4. Data Flow

```
TransactionsPageService
├── Injects: TransactionsClient, SpendingClient
│
├── loading()  → true while any of 4 resources has not returned a value
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

All four resources fire on component init. The transaction table resource supports pagination — page turns show `resource.isLoading()` on the table only.

---

## 5. API Changes Required

### 5.1 New Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `GET api/transactions/summary?TimePeriod=` | Transactions | Row 1 summary cards |
| `GET api/transactions/dailySummary?TimePeriod=` | Transactions | Row 2 daily spend + count chart |

### 5.2 Existing Endpoint Extensions

The existing `GET api/transactions` endpoint already supports the required query parameters (`page`, `pageSize`, `sortField`, `sortDirection`, `startDate`, `endDate`, `minAmount`, `maxAmount`, `transactionTypeCode`, `transactionCategoryCode`). No backend changes needed for the table.

### 5.3 New TypeScript Interfaces

| Type | Location | Description |
|---|---|---|
| `TransactionSummaryResponse` | `shared/api/transactions/transaction-summary.ts` | Summary stats for Row 1 |
| `TransactionPeriodSummary` | `shared/api/transactions/transaction-summary.ts` | Period-level summary |
| `DailyTransactionSummary` | `shared/api/transactions/daily-transaction-summary.ts` | Daily spend + count |
| `DailySummaryResponse` | `shared/api/transactions/daily-transaction-summary.ts` | Wrapper for daily summaries |

### 5.4 Client Changes

`TransactionsClient` (in `features/transactions/data/transactions-client.ts`) needs two new `httpResource` factory methods:

```typescript
getSummary(period: signal<TimePeriod>): HttpResource<TransactionSummaryResponse>
getDailySummary(period: signal<TimePeriod>): HttpResource<DailySummaryResponse>
```

---

## 6. Period Selector Behavior

- Defaults to `Monthly`
- Affects all data sources simultaneously
- No custom date range on this page (filter bar's date range pickers handle granular filtering)

---

## 7. Migration from Current Implementation

### What Changes

| Current | New |
|---|---|
| No Row 1 summary cards | 4 summary cards (Total Transactions, Total Spend, Total Income, Net Flow) |
| No Row 2 charts | Daily spend chart + category donut |
| Infinite scroll card list | Paginated table with page numbers, size selector |
| "Load More" button | `PaginationControls` shared component |
| `TransactionCard` component | `TransactionTable` shared component |
| FilterBar feature-local | FilterBar moved to `shared/ui/filters/` |

### What Stays

- Filter bar functionality (date range, amount, type, category, search)
- TransactionsClient base structure (thin wrapper around HttpClient)
- Page service pattern (loading/error gates, computed signals, exposed resources)

---

## 8. Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` ×4 for Row 1, chart skeletons for Row 2, `TransactionTableSkeleton` for Row 3.
- **Error**: All components hidden. `ErrorState` with retry button centered on page.

### Subsequent Fetch (Period Change / Filter Change)

- **Error on Row 1 or Row 2 resources**: Current data remains visible. Toast: "Failed to refresh data. Click to retry."
- **Error on Row 3 (table fetch)**: Table shows inline error with retry button. Rest of page unchanged.

### No Data

- **No transactions for period**: Cards show `$0.00` / `0 count`. Charts show "No data". Table shows centered empty message.
- **Filter yields no results**: Table shows "No transactions match your filters" with a "Clear filters" button.

---

## 9. Future-Proofing Notes

- **TransactionTable reusability**: The table must accept configurable columns via input. The Spending and Account pages will reuse this table but with fewer columns (no Type column on Spending page).
- **OpenAPI codegen**: The new `TransactionSummaryResponse` and `DailySummaryResponse` types are the scaffolding for auto-generated types. Backend DTOs should mirror these interfaces.
- **Export functionality**: Future enhancement — CSV export of filtered transactions. The table component should expose a method to return the current filtered dataset.
- **Bulk operations**: Future enhancement — category reassignment, merge duplicates. Table rows should support selection (checkbox column, configurable via input).
