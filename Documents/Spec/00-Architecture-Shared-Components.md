# Architecture & Shared Components Specification

## Status: Approved

## Purpose

Defines the shared architectural patterns, component inventory, and conventions that all four dashboard pages (Home, Transactions, Spending, Account) follow. This is the foundation spec — page-level specs reference this document for shared behavior.

---

## 1. Page Service Pattern

Every page has a single `*PageService` that owns all data loading, error handling, and computed signals for that page.

### 1.1 Three-Layer Signal Model

**Layer 1 — Initial load gates**

```typescript
readonly loading = computed(() =>
  !this.resourceA.hasValue() ||
  !this.resourceB.hasValue() ||
  !this.resourceC.hasValue(),
);

readonly error = computed(() =>
  (this.resourceA.error() !== null && !this.resourceA.hasValue()) ||
  (this.resourceB.error() !== null && !this.resourceB.hasValue()) ||
  (this.resourceC.error() !== null && !this.resourceC.hasValue()),
);
```

- `loading()`: `true` while any resource has not yet returned a value. Flips to `false` once all resources have loaded at least once. Stays `false` during refetches (period change, filter change, institution change).
- `error()`: `true` only if a resource failed before ever returning a value. Does NOT flip on subsequent fetch failures.

**Layer 2 — Transformed data signals**

Each signal is a `computed()` over its resource value. The page service does all mapping — the component only binds.

```typescript
readonly currentNetWorth = computed(
  () => this.balanceSummaryResource.value()?.currentPeriodBreakdown.balance ?? 0,
);
```

**Layer 3 — Exposed resources**

Sub-components (paginated tables, independently loaded sections) check `resource.isLoading()` for their own loading state.

```typescript
readonly transactionsResource = httpResource<ListResult<Transaction>>(...);
```

### 1.2 Period Signal Ownership

The page component owns the `selectedPeriod` signal. The page service takes the period as a signal dependency for its `httpResource` factories.

```typescript
// Page component
readonly selectedPeriod = signal<TimePeriod>('Monthly');

// Page service — resource URL depends on period signal
readonly balanceSummaryResource = httpResource<AccountAnalyticsResponse>({
  params: () => ({ TimePeriod: period() }),
  // ...
});
```

### 1.3 Future-Proofing

- Page services must not hardcode resource counts in `loading()` / `error()`. If a new resource is added, the gates must be updated. Consider a helper that takes an array of resources.
- Page services must not know about UI concerns (skeletons, toasts, routing). They own data, not presentation.

---

## 2. Shared Component Inventory

### 2.1 Existing — Already in `shared/ui/`

| Component | Path | Inputs | Used By |
|---|---|---|---|
| `SummaryCard` | `shared/ui/cards/summary-card/` | `title`, `currentValue`, `lastMonthValue`, `showTopBar` | Home (Row 1), Transactions (Row 1), Spending (Row 1) |
| `SummaryCardSkeleton` | `shared/ui/cards/summary-card-skeleton/` | — | All pages |
| `TransactionsList` | `shared/ui/transactions/transactions-list/` | `transactions: Transaction[]` | Home (Row 3) |
| `TransactionItem` | `shared/ui/transactions/transaction-item/` | `transaction: Transaction` | TransactionsList |
| `TransactionsListSkeleton` | `shared/ui/transactions/transactions-list-skeleton/` | — | Home (Row 3) |
| `AccountItem` | `shared/ui/accounts/account-item/` | `account: Account` | Account page |

### 2.2 To Be Moved to `shared/ui/`

| Component | Current Path | Target Path | Reason |
|---|---|---|---|
| `PeriodSelector` | `features/spending/components/period-selector/` | `shared/ui/period-selector/` | Used by all 4 pages |
| `FilterBar` | `features/transactions/components/filter-bar/` | `shared/ui/filters/filter-bar/` | Used by Transactions + Account |
| `CategoryPieChart` | `features/spending/components/category-pie-chart/` | `shared/ui/charts/category-pie-chart/` | Used by Spending + Account |
| `CategoryBreakdownList` | `features/spending/components/category-breakdown-list/` | `shared/ui/charts/category-breakdown-list/` | Used by Spending + Account |
| `SpendActivityChart` | `features/home/components/spend-activity-chart/` | `shared/ui/charts/spend-activity-chart/` | Used by Home + Account |

### 2.3 New Shared Components

| Component | Path | Purpose | Pages |
|---|---|---|---|
| `InfoCard` | `shared/ui/cards/info-card/` | Non-currency metric card (text label + amount/value) | Spending (Row 1 — Top Category) |
| `InfoCardSkeleton` | `shared/ui/cards/info-card-skeleton/` | Loading placeholder for InfoCard | Spending |
| `PaginationControls` | `shared/ui/data-table/pagination-controls/` | Page numbers, size selector, info text | Transactions, Spending, Account |
| `TransactionTable` | `shared/ui/data-table/transaction-table/` | Full-featured paginated table with sorting | Transactions (Row 3), Spending (Row 3), Account (Row 3) |
| `TransactionTableSkeleton` | `shared/ui/data-table/transaction-table-skeleton/` | Loading placeholder | All pages with Row 3 |
| `EmptyState` | `shared/ui/empty-state/` | Centered "no data" message with optional CTA | All pages |
| `ErrorState` | `shared/ui/error-state/` | Generic error message with retry button | All pages (initial load error) |
| `DailySpendChart` | `shared/ui/charts/daily-spend-chart/` | Dual-axis: area chart + bubble markers for transaction count | Transactions (Row 2 left) |
| `SpendingTrendChart` | `shared/ui/charts/spending-trend-chart/` | Area chart of daily spending | Spending (Row 2 left), Account (Row 2 left) |

### 2.4 SummaryCard Extension — Period-Agnostic Delta

The existing `SummaryCard` shows `% change vs last month`. This must become period-agnostic:

- **Current inputs**: `currentValue`, `lastMonthValue`
- **Required change**: Rename `lastMonthValue` to `previousValue`. The delta calculation is the same — the label should reflect the active period (e.g., "vs last week", "vs last quarter", "vs last year").
- **New input**: `periodLabel: string` — dynamic label for the comparison period.
- **Delta logic**: `(currentValue - previousValue) / previousValue * 100`. Handle `previousValue === 0` gracefully (show "—" or "N/A").

### 2.5 InfoCard Design

For metrics that don't fit the currency + delta pattern (e.g., "Top Category" shows a category name + its spend amount):

```typescript
@Component({
  selector: 'app-info-card',
  // ...
})
export class InfoCard {
  @Input() title = '';         // "Top Category"
  @Input() value = '';         // "Groceries"
  @Input() subValue = '';      // "$1,234.56"
  @Input() previousValue = ''; // "Dining"
  @Input() previousSubValue = ''; // "$987.00"
}
```

Card displays current value prominently, previous period value in footer. No percentage delta — the metric type doesn't support it.

---

## 3. Toast Notification Service

### 3.1 Design

A shared, injectable service that any component or service can use to show non-intrusive notifications.

```typescript
@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<ToastMessage[]>([]);

  show(message: string, options?: ToastOptions): void { ... }
  dismiss(id: string): void { ... }
  clear(): void { ... }
}

interface ToastMessage {
  id: string;
  message: string;
  type: 'error' | 'info' | 'success';
  autoDismiss?: number;    // ms, default 5000
  action?: { label: string; onClick: () => void };
}
```

### 3.2 Integration with Page Services

Each page service wires resource errors to the ToastService via `effect()`:

```typescript
effect(() => {
  const err = this.spendingResource.error();
  if (err !== null && this.spendingResource.hasValue()) {
    this.toastService.show('Failed to refresh spending data.', {
      type: 'error',
      action: { label: 'Retry', onClick: () => this.spendingResource.reload() },
    });
  }
});
```

Key: `hasValue()` guard ensures toasts only fire on **subsequent** fetch failures, not initial load failures.

### 3.3 Toast Container

A single `ToastContainer` component rendered in `app.html` (shell level) that reads from `ToastService.toasts()` signal. Auto-dismiss via `setTimeout`, manual dismiss via `dismiss()` call.

---

## 4. Chart Conventions

### 4.1 Library

All charts use Apache ECharts via `ngx-echarts`. Chart types already registered: `LineChart`, `BarChart`, `PieChart`.

### 4.2 Donut Chart Standard

All donut charts share the same base configuration:

- `radius: ['40%', '70%']`
- `center: ['50%', '50%']`
- Labels hidden (`label.show: false`)
- Tooltip: `'{b}: ${c} ({d}%)'`
- Center text via `graphic` element showing total value
- Click handler emits category code for drill-down filtering

### 4.3 Trend Chart Standard

All line/area trend charts share the same pattern:

- Current period: solid line, primary color, area fill with gradient
- Previous period: dashed line, muted color, lighter gradient fill
- X-axis: hidden labels, tooltip on hover
- Y-axis: hidden axis, values in tooltip
- Grid: full bleed (`containLabel: false`)

### 4.4 Category Color Mapping

Consistent color assignment across all donut charts. Lives in `shared/data/category-colors.ts`:

```typescript
export const CATEGORY_COLORS: Record<string, string> = {
  UNC: '#9CA3AF',    // Uncategorized — gray
  AUTO: '#3B82F6',   // Auto & Transport — blue
  BILLS: '#EF4444',  // Bills & Utilities — red
  DINING: '#F59E0B', // Dining — amber
  GROCERIES: '#10B981', // Groceries — emerald
  // ... all 24+ categories
};
```

### 4.5 Theme Awareness

Charts adapt to light/dark theme via `ThemeService`. Chart background, grid lines, and text colors must use theme tokens, not hardcoded values.

---

## 5. Skeleton Loading Pattern

Every component that displays async data has a matching skeleton component:

| Component | Skeleton |
|---|---|
| `SummaryCard` | `SummaryCardSkeleton` |
| `InfoCard` | `InfoCardSkeleton` |
| `*Chart` | `*ChartSkeleton` |
| `TransactionTable` | `TransactionTableSkeleton` |
| `TransactionsList` | `TransactionsListSkeleton` |
| `CategoryBreakdownList` | `CategoryBreakdownListSkeleton` |
| `FilterBar` | — (no skeleton, renders immediately) |

Skeletons use `hlmSkeleton` from spartan-ng with dimensions matching the real component. Page-level skeleton layout renders when `loading()` is `true`.

---

## 6. API Client Conventions

### 6.1 Location

API clients live in `features/*/data/` (current convention). Each client is a thin `@Injectable({ providedIn: 'root' })` that exposes:

- `httpResource` factory methods for GET endpoints
- Observable-based methods for POST/PUT/DELETE mutations

### 6.2 Response Types

TypeScript interfaces that mirror backend DTOs live in `shared/api/`, organized by domain:

```
shared/api/
├── list-result.ts                          # ListResult<T>
├── transactions/
│   ├── transactions.ts                     # Transaction
│   ├── transaction-summary.ts              # TransactionSummaryResponse, TransactionPeriodSummary
│   └── daily-transaction-summary.ts        # DailyTransactionSummary, DailySummaryResponse
├── accounts/
│   ├── account.ts                          # Account
│   ├── account-analytics.ts                # AccountAnalyticsResponse, etc.
│   └── institution.ts                      # InstitutionSummary, InstitutionSummaryResponse
└── spending/
    ├── spending-by-period.ts               # SpendingByPeriod, SpendingActivity
    └── spending-over-time-summary.ts       # SpendingOverTimeSummary
```

### 6.3 Future-Proofing

- Clients must not embed business logic. They are thin wrappers around `httpResource` / `HttpClient`.
- Response types must be immutable interfaces (no setters, no mutable state).
- New endpoints should follow existing naming: `{domain}-client.ts` for the service, co-located type files.

---

## 7. Filter Bar Conventions

### 7.1 Shared FilterBar Component

Moved to `shared/ui/filters/filter-bar/`. Accepts configuration inputs to control which filters appear:

```typescript
@Component({
  selector: 'app-filter-bar',
  // ...
})
export class FilterBar {
  @Input() showDateRange = true;
  @Input() showAmountRange = true;
  @Input() showTransactionType = true;
  @Input() showCategory = true;
  @Input() showSearch = true;
  @Input() showAccount = false;    // enabled for Account page

  readonly filterChange = output<FilterState>();
  readonly reset = output<void>();
}

interface FilterState {
  startDate?: Date;
  endDate?: Date;
  minAmount?: number;
  maxAmount?: number;
  transactionTypeCode?: string;
  transactionCategoryCode?: string;
  search?: string;
  accountId?: string;
}
```

### 7.2 Filter Behavior

- Filters are NOT synced to URL query params.
- Apply button triggers the filter. Reset button clears all filters and re-emits empty state.
- Filter state is owned by the page component, passed to the page service, which passes it to the API client.
- When filters change, the table shows local loading (`resource.isLoading()`), not page-level loading.

---

## 8. Data Map Architecture

Static data maps live in `shared/data/`:

| File | Contents |
|---|---|
| `account-type-map.ts` | `ACCOUNT_TYPE_CODES`, `ACCOUNT_TYPE_LABELS`, `getAccountTypeLabel()` |
| `category-map.ts` | `CATEGORY_CODE_TO_NAME` (24+ categories), `SPENDING_CATEGORY_CODES`, `getCategoryName()` |
| `category-colors.ts` | `CATEGORY_COLORS` — consistent donut chart colors |
| `transaction-type-map.ts` | `TRANSACTION_TYPE_OPTIONS` (Credit/Debit) |
| `time-period.ts` | `TimePeriod` type, `TIME_PERIODS` array, `DEFAULT_TIME_PERIOD`, validators |

**Future enhancement**: These maps can be replaced by a runtime config endpoint (`GET api/config`) that returns the full app configuration. The current static files are the scaffolding for that transition.

---

## 9. Error Handling Strategy

| Scenario | Behavior |
|---|---|
| **Initial load fails** (any resource) | Hide all components. Show `ErrorState` with retry button. |
| **Subsequent fetch fails** (period/filter change, Row 1 or Row 2) | Current data remains visible. Toast notification: "Failed to refresh data. Click to retry." |
| **Subsequent fetch fails** (paginated table) | Table shows inline error with retry button. Rest of page unchanged. |
| **Toast behavior** | Auto-dismiss after 5s. Clicking toast re-triggers the failed resource. |

---

## 10. Responsive Layout Conventions

| Breakpoint | Summary Cards | Charts | Detail Table |
|---|---|---|---|
| Desktop (≥1280px) | 4 columns | 2 columns side by side | Full width |
| Tablet (768px–1279px) | 2 columns | Stacked | Full width |
| Mobile (<768px) | 1 column | Stacked | Full width, horizontal scroll on table |

All grids use Tailwind responsive prefixes:
- Cards: `grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4`
- Charts: `grid grid-cols-1 xl:grid-cols-2 gap-4`

---

## 11. Typography & Color Conventions

### Typography

| Element | Class |
|---|---|
| Card title | `text-xs font-medium text-muted-foreground` |
| Card value | `text-2xl font-semibold` |
| Card delta | `text-xs text-emerald-500` / `text-red-500` |
| Chart axis labels | `text-xs text-muted-foreground` |
| Table header | `text-xs font-medium uppercase tracking-wider` |
| Table cell | `text-sm` |

### Color

| Meaning | Class / Value |
|---|---|
| Positive change (income ↑, net worth ↑, spend ↓) | `emerald-500` |
| Negative change (spend ↑, net worth ↓, income ↓) | `red-500` |
| Current period chart series | Primary color (theme-dependent) |
| Previous period chart series | Muted color, dashed line |
| Income amount | `text-emerald-500` |
| Spend amount | `text-red-500` |

**Delta direction by card type:**

| Card | Green means | Red means |
|---|---|---|
| Net Worth / Checking / Savings / Investments | Growth | Decline |
| Total Spend | Less spending | More spending |
| Total Income | More income | Less income |
| Net Flow | Positive flow | Negative flow |
| Daily Average | Lower avg | Higher avg |
