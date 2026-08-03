# BasicFinance Dashboard Design

## Overview

BasicFinance is a personal finance dashboard with four primary pages: **Home**, **Transactions**, **Spending**, and **Account**. This document defines the shared layout conventions, component patterns, and data flow that all pages follow.

## Architecture

### Directory Structure

```
src/app/
├── core/
│   └── api/                    # API clients + types (vertical slice clients)
│       ├── list-result.ts      # Shared paginated response type
│       ├── accounts/           # Account client + AccountAnalyticsResponse, etc.
│       ├── spending/           # Spending client + SpendingByPeriod, etc.
│       ├── transactions/       # Transactions client + Transaction, etc.
│       └── spreadsheets/       # Settings client + Spreadsheet, etc.
├── features/
│   ├── home/
│   │   ├── home.ts             # Page component
│   │   ├── home-page-service.ts # Page-level data aggregation service
│   │   └── components/         # Feature-specific UI components
│   ├── transactions/
│   │   ├── transactions.ts
│   │   ├── transactions-page-service.ts
│   │   └── components/
│   ├── spending/
│   │   ├── spending.ts
│   │   ├── spending-page-service.ts
│   │   └── components/
│   ├── account/
│   │   ├── account.ts
│   │   ├── account-page-service.ts
│   │   └── components/
│   └── settings/
│       ├── settings.ts
│       ├── settings-page-service.ts
│       └── components/
└── shared/
    ├── ui/                     # Reusable UI components
    ├── data/                   # Static data (maps, constants)
    ├── pipes/                  # Shared pipes
    └── models/                 # Non-API models (nav items, menu items)
```

### Data Flow Layers

```
┌─────────────────────────────────────────────────────────────┐
│  Page Component (home.ts)                                   │
│  - Owns period/filter signals                               │
│  - Injects PageService                                      │
│  - Renders based on page service signals                     │
├─────────────────────────────────────────────────────────────┤
│  Page Service (home-page-service.ts)                         │
│  - Injects API clients from core/api/                        │
│  - Owns httpResources bound to period/filter signals         │
│  - Exposes: loading() — initial load gate                    │
│  - Exposes: error() — initial load failure gate              │
│  - Exposes: computed data signals (e.g. currentNetWorth)     │
│  - Exposes: individual resources for sub-component loading   │
├─────────────────────────────────────────────────────────────┤
│  API Clients (core/api/transactions/transactions-client.ts)  │
│  - Thin layer: httpResource factories + mutation methods     │
│  - Co-located with their response types                      │
├─────────────────────────────────────────────────────────────┤
│  API Types (core/api/transactions/transactions.ts)           │
│  - TypeScript interfaces mirroring backend DTOs              │
└─────────────────────────────────────────────────────────────┘
```

### Page State Model

Each page service exposes three layers of state:

**1. Initial load gate — `loading()` and `error()`**

```typescript
// loading — true while any resource has not yet returned a value
// Stays false after initial load (httpResource.hasValue() persists during refetches)
readonly loading = computed(() =>
  !this.balanceSummaryResource.hasValue() ||
  !this.spendingOverTimeResource.hasValue() ||
  !this.transactionsResource.hasValue(),
);

// error — true if any resource failed BEFORE ever returning a value
readonly error = computed(() =>
  (this.balanceSummaryResource.error() !== null && !this.balanceSummaryResource.hasValue()) ||
  (this.spendingOverTimeResource.error() !== null && !this.spendingOverTimeResource.hasValue()) ||
  (this.transactionsResource.error() !== null && !this.transactionsResource.hasValue()),
);
```

- **Initial load failure**: All components are hidden. A generic error message with retry button is shown.
- **Subsequent fetch failure** (period change, filter change): Current data remains visible. A toast notification appears to inform the user. The `error()` signal does NOT flip — the page stays in a healthy state.

**2. Transformed data signals — computed from raw resources**

```typescript
// Each signal is a derived computation over its resource
readonly currentNetWorth = computed(
  () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.balance ?? 0,
);
readonly recentTransactions = computed(
  () => this.transactionsResource.value()?.items ?? [],
);
```

**3. Exposed resources — for sub-component independent loading**

```typescript
// Paginated table checks this for page-turn loading state
readonly transactionsResource = httpResource<ListResult<Transaction>>(...);
```

A paginated table can show its own loading spinner on page turn (`resource.isLoading()`) without affecting `loading()` or the rest of the page.

### Toast Notifications

Subsequent fetch errors surface as non-intrusive toast notifications:

- **Trigger**: Any `httpResource` error after the initial page load has completed.
- **Behavior**: Toast appears at top of page, auto-dismisses after 5 seconds, user can also manually dismiss.
- **Content**: Generic message — "Failed to refresh data. Click to retry."
- **Retry**: Clicking the toast re-triggers the failed resource.

## Design Goals

- **Consistency**: Every page follows the same row-based layout pattern so users develop muscle memory.
- **Scannability**: Key metrics at the top, trends in the middle, details at the bottom.
- **Period-aware**: All data is scoped to a user-selected time period. Default is `Monthly`.
- **Responsive**: Cards collapse from 4-column → 2-column → 1-column as viewport shrinks.
- **Single source of state**: Each page has one `PageService` that owns loading/error/data for the initial load.

## Shared Layout Pattern

Every page uses a 3-row layout within a `flex flex-col gap-4` container:

```
┌─────────────────────────────────────────────────────────┐
│  ROW 1 — Summary Cards (grid-cols-4 → 2 → 1)           │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐      │
│  │ Metric  │ │ Metric  │ │ Metric  │ │ Metric  │      │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘      │
├─────────────────────────────────────────────────────────┤
│  ROW 2 — Charts & Breakdowns (grid-cols-2 → 1)         │
│  ┌──────────────────────┐ ┌──────────────────────┐     │
│  │ Primary Chart        │ │ Secondary Breakdown  │     │
│  │ (trend / comparison) │ │ (donut / bar / list) │     │
│  └──────────────────────┘ └──────────────────────┘     │
├─────────────────────────────────────────────────────────┤
│  ROW 3 — Detail Table (full width)                      │
│  ┌──────────────────────────────────────────────────┐   │
│  │ Paginated, filterable data table                 │   │
│  └──────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
```

### Row 1 — Summary Cards

- **Grid**: `grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4`
- **Component**: `SummaryCard` (shared, from `shared/ui/cards/`)
- **Behavior**: Shows current period value + percentage change vs previous period
- **Skeleton**: `SummaryCardSkeleton` during loading
- **Count**: 3-4 cards per page (varies by page)

### Row 2 — Charts & Breakdowns

- **Grid**: `grid grid-cols-1 xl:grid-cols-2 gap-4`
- **Left card**: Primary chart (line, area, or dual-axis) showing trends over time
- **Right card**: Secondary visualization (donut chart, bar chart, or breakdown list)
- **Height**: Charts should be at least 320px tall for readability
- **Skeleton**: Each chart has a matching `*Skeleton` component

### Row 3 — Detail Table

- **Full width** card containing a paginated table
- **Pagination**: Traditional page numbers with page size selector (20 items default)
- **Filters**: Page-specific filter bar above the table
- **Sorting**: Clickable column headers, default sort by date descending
- **Empty state**: Centered message when no data exists for the period
- **Initial load**: First page of data is part of the initial load gate (`loading()`)
- **Page turns**: Show local loading indicator on table only (`resource.isLoading()`)

## Shared Components

### Period Selector

A shared component (`shared/ui/period-selector/`) that appears in the page header:

- **Options**: Weekly, Monthly (default), Quarterly, Yearly
- **Transactions page**: Extended to include custom date range (start/end date pickers)
- **State**: Stored as a signal on the page component, propagated to the page service
- **Previous period**: Automatically calculated (e.g., if current = Aug 2025, previous = Jul 2025)

### Card Wrapper

All row content lives inside `hlmCard` sections. Card conventions:
- `hlmCardHeader` — title
- `hlmCardContent` — primary content
- `hlmCardFooter` — secondary info (e.g., "vs last period" delta)

### Data Loading Pattern

All pages use the same two-layer pattern:

```typescript
// 1. Page component owns signals
readonly selectedPeriod = signal<TimePeriod>('Monthly');

// 2. Page service exposes loading, error, and transformed data signals
readonly loading = this.pageService.loading;
readonly error = this.pageService.error;
readonly currentNetWorth = this.pageService.currentNetWorth;

// 3. Sub-components that fetch independently (e.g., pagination) use exposed resources
readonly tableResource = this.pageService.transactionsResource;
```

Template rendering:

```html
@if (loading()) {
  <skeleton-layout />
} @else if (error()) {
  <error-state />
} @else {
  <!-- real content, bound to page service data signals -->
  <summary-card [value]="currentNetWorth()" />

  <!-- Table checks its own resource for page-turn loading -->
  <transaction-table [resource]="tableResource" />
}
```

## Chart Library

- **Engine**: Apache ECharts via `ngx-echarts`
- **Theme**: Chart colors adapt to light/dark theme via `ThemeService`
- **Donut charts**: Center text shows total value or top category
- **Line/Area charts**: Current period = solid line, previous period = dashed line with gradient fill

## Typography & Sizing

| Element | Class |
|---|---|
| Card title | `text-xs font-medium text-muted-foreground` |
| Card value | `text-2xl font-semibold` |
| Card delta | `text-xs text-emerald-500` / `text-red-500` |
| Chart axis labels | `text-xs text-muted-foreground` |
| Table header | `text-xs font-medium uppercase tracking-wider` |
| Table cell | `text-sm` |

## Color Conventions

- **Positive change** (income increase, net worth growth): `emerald-500`
- **Negative change** (spend increase, net worth decline): `red-500`
- **Current period chart series**: Primary color (theme-dependent)
- **Previous period chart series**: Muted color with dashed line style
- **Donut segments**: Consistent category-to-color mapping across all pages

## Account Type Codes

| Code | Label | Used In |
|---|---|---|
| `CHK` | Checking | All pages |
| `SAV` | Savings | All pages |
| `INV` | Investments | All pages |
| `CRD` | Credit Cards | Account page (balance shown as negative) |

## Time Periods

| Period | Default | Description |
|---|---|---|
| `Weekly` | — | Last 7 days |
| `Monthly` | Yes | Current calendar month |
| `Quarterly` | — | Current quarter |
| `Yearly` | — | Current calendar year |

Custom date ranges are supported on the Transactions page only.
