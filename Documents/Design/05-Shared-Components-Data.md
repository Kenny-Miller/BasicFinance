# Shared Components & Data Architecture

## Shared UI Components

Components that are reused across multiple pages should live in `shared/ui/`. Feature-specific components live in `features/<page>/components/`.

### Existing Shared Components

| Component | Location | Used By |
|---|---|---|
| `SummaryCard` | `shared/ui/cards/` | Home, Transactions, Spending, Account (all Row 1) |
| `SummaryCardSkeleton` | `shared/ui/cards/` | All pages |
| `TransactionCard` | `shared/ui/transactions/` | Home (recent transactions) |
| `TransactionsListSkeleton` | `shared/ui/transactions/` | Transactions |

### Components to Move to Shared

| Component | Current Location | Target Location | Reason |
|---|---|---|---|
| `PeriodSelector` | `features/spending/components/` | `shared/ui/period-selector/` | Used by all 4 pages |
| `CategoryPieChart` | `features/spending/components/` | `shared/ui/charts/` | Used by Spending + Account pages |
| `CategoryBreakdownList` | `features/spending/components/` | `shared/ui/charts/` | Used by Spending + Account pages |
| `SpendActivityChart` | `features/home/components/` | `shared/ui/charts/` | Used by Home + Account pages |
| `FilterBar` | `features/transactions/components/` | `shared/ui/filters/` | Used by Transactions + Account pages |
| `RecentTransactions` | `features/home/components/` | `shared/ui/transactions/` | Could be reused on Account page |

### New Shared Components Needed

| Component | Purpose | Pages |
|---|---|---|
| `PaginationControls` | Page numbers, size selector, info text | Transactions, Spending, Account |
| `TransactionTable` | Full-featured paginated table with sorting | Transactions, Spending, Account |
| `EmptyState` | Centered "no data" message with optional CTA | All pages |
| `ErrorState` | Generic error message with retry button | All pages (initial load error) |
| `Toast` | Non-intrusive notification for subsequent fetch errors | All pages |

## Data Architecture

### Core API Clients

All API clients and their response types live in `core/api/`, organized by domain:

```
core/api/
├── list-result.ts                          # Shared paginated response type
├── accounts/
│   ├── accounts-client.ts                  # AccountClient service
│   ├── account-analytics.ts                # AccountAnalyticsResponse, etc.
│   ├── account.ts                          # AccountDto
│   └── account-by-type.ts                  # AccountByType types
├── spending/
│   ├── spending-client.ts                  # SpendingClient service
│   ├── spending-by-period.ts               # SpendingByPeriod
│   └── spending-over-time-summary.ts       # SpendingOverTimeSummary
├── transactions/
│   ├── transactions-client.ts              # TransactionsClient service
│   └── transactions.ts                     # Transaction DTO
└── spreadsheets/
    ├── spreadsheets-client.ts              # SpreadsheetsClient (Settings mutations)
    └── spreadsheet.ts                      # Spreadsheet DTO
```

Each client is a thin `@Injectable` that exposes:
- `httpResource` factory methods for GET endpoints
- Observable-based methods for POST/PUT/DELETE mutations

### Page Services

Each page has a `*PageService` that lives in the feature root (not in a `data/` subfolder):

```
features/home/
├── home.ts
├── home-page-service.ts          ← loading(), error(), computed data signals
└── components/

features/transactions/
├── transactions.ts
├── transactions-page-service.ts  ← loading(), error(), computed data signals
└── components/
```

### Page Service Pattern

Each page service exposes three categories of signals:

**1. Initial load gates**

```typescript
// loading — true while any resource has not yet returned a value
// Stays false after initial load (httpResource.hasValue() persists during refetches)
readonly loading = computed(() =>
  !this.resourceA.hasValue() ||
  !this.resourceB.hasValue() ||
  !this.resourceC.hasValue(),
);

// error — true if any resource failed BEFORE ever returning a value
readonly error = computed(() =>
  (this.resourceA.error() !== null && !this.resourceA.hasValue()) ||
  (this.resourceB.error() !== null && !this.resourceB.hasValue()) ||
  (this.resourceC.error() !== null && !this.resourceC.hasValue()),
);
```

Key behavior: Once all resources have loaded at least once, `loading()` stays `false` and `error()` stays `false` — even during refetches triggered by period changes, filter changes, or institution changes. A refetch error does not flip the initial load gates.

**2. Transformed data signals**

```typescript
readonly currentNetWorth = computed(
  () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.balance ?? 0,
);
readonly transactions = computed(
  () => this.transactionsResource.value() ?? null,  // ListResult<Transaction>
);
```

Each signal is a derived computation over its resource. The service does the mapping — the component just binds.

**3. Exposed resources**

```typescript
readonly transactionsResource = httpResource<ListResult<Transaction>>(...);
```

Sub-components (paginated tables) check `resource.isLoading()` for their own loading state on page turns and filter changes.

### Template Pattern

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

  <!-- Row 3 — table manages its own loading for page turns -->
  <transaction-table
    [transactions]="transactions()"
    [resource]="transactionsResource" />
}
```

### API Endpoints Required

#### Existing Endpoints

| Endpoint | Method | Used By |
|---|---|---|
| `GET api/accounts/balanceSummary?TimePeriod=` | Home | Home page |
| `GET api/transactions?page=&pageSize=&sortField=&sortDirection=&...` | All | All pages |
| `GET api/Spending/SpendingOverTimeSummary` | Home | Home page |
| `GET api/Spending/SpendingActivityByPeriod?startDate=&spendingPeriod=` | Spending | Spending page |

#### New Endpoints Needed

| Endpoint | Method | Purpose |
|---|---|---|
| `GET api/transactions/summary?TimePeriod=&startDate=&endDate=` | Transactions | Row 1 summary cards |
| `GET api/transactions/dailySummary?TimePeriod=&startDate=&endDate=` | Transactions | Row 2 daily spend + count chart |
| `GET api/accounts/institutions` | Account | Sidebar institution list |
| `GET api/accounts/institution/{id}/summary?TimePeriod=` | Account | Row 1 cards + account list |
| `GET api/Spending/SpendingOverTimeSummary?institutionId=` | Account | Row 2 left chart |
| `GET api/Spending/SpendingActivityByPeriod?institutionId=&...` | Account | Row 2 right chart |
| `GET api/transactions?institutionId=&...` | Account | Row 3 transaction table |

The existing endpoints need to be extended to support `institutionId` filtering for the Account page.

### Paginated Table Initial Load

The first page of transaction data is part of the initial load gate. The `transactionsResource` is included in `loading()` and `error()` checks. After initial load, page turns and filter changes use `resource.isLoading()` for local loading state.

### Error Handling Strategy

| Scenario | Behavior |
|---|---|
| **Initial load fails** (any resource) | Hide all components. Show generic "An error occurred" message with retry button. |
| **Subsequent fetch fails** (period/filter change on Row 1 or Row 2) | Keep current data visible. Show toast notification: "Failed to refresh data. Click to retry." |
| **Subsequent fetch fails** (paginated table) | Table shows inline error with retry. Rest of page unchanged. |
| **Toast behavior** | Auto-dismiss after 5s. Clicking toast re-triggers the failed resource. |

### Response Types

All API responses use consistent types:

| Type | Location | Description |
|---|---|---|
| `ListResult<T>` | `core/api/list-result.ts` | Paginated response |
| `Transaction` | `core/api/transactions/` | Transaction DTO |
| `SpendingByPeriod` | `core/api/spending/` | Category breakdown |
| `SpendingOverTimeSummary` | `core/api/spending/` | Daily spending arrays |
| `AccountAnalyticsResponse` | `core/api/accounts/` | Balance breakdown |

#### New Response Types Needed

| Type | Description |
|---|---|
| `TransactionSummaryResponse` | Summary stats for Transactions Row 1 |
| `DailyTransactionSummary` | Daily spend + count for Transactions Row 2 |
| `InstitutionSummary` | Institution list for Account sidebar |
| `InstitutionSummaryResponse` | Institution-scoped account data |

## Chart Consistency

### Donut Chart Configuration

All donut charts use the same ECharts configuration:

```typescript
{
  series: [{
    type: 'pie',
    radius: ['40%', '70%'],
    center: ['50%', '50%'],
    label: { show: false },
    tooltip: {
      trigger: 'item',
      formatter: '{b}: ${c} ({d}%)'
    }
  }],
  graphic: [{
    type: 'text',
    left: 'center',
    top: 'center',
    style: {
      text: '$XX,XXX',
      fontSize: 20,
      fontWeight: 'bold'
    }
  }]
}
```

### Line/Area Chart Configuration

All trend charts use the same pattern:

- Current period: Solid line, primary color, area fill with gradient
- Previous period: Dashed line, muted color, area fill with lighter gradient
- X-axis: Hidden labels, tooltip on hover
- Y-axis: Hidden axis, values in tooltip
- Grid: Full bleed (no padding)

### Category Color Mapping

Consistent color assignment across all donut charts:

```typescript
const CATEGORY_COLORS: Record<string, string> = {
  UNC: '#9CA3AF',    // gray
  AUTO: '#3B82F6',   // blue
  BILLS: '#EF4444',  // red
  DINING: '#F59E0B', // amber
  GROCERIES: '#10B981', // emerald
  // ... all 24 categories
};
```

This mapping should live in `shared/data/category-colors.ts`.

## Skeleton Loading

Every component has a matching skeleton:

| Component | Skeleton |
|---|---|
| `SummaryCard` | `SummaryCardSkeleton` |
| `*Chart` | `*ChartSkeleton` |
| `*Table` | `*TableSkeleton` |
| `*List` | `*ListSkeleton` |

Skeletons use `hlmSkeleton` from spartan-ng with appropriate dimensions matching the real component.
