# Account Page Specification

## Status: Approved

## Purpose

The Account page provides a per-institution view of all associated accounts, their balances, spending patterns, and transaction history. Users navigate between institutions via the shell sidebar, which dynamically lists all institutions for the authenticated user.

**Implementation status**: Placeholder only. Requires full build — new components, new API endpoints, and new `Institution` entity on the backend.

---

## 1. Route

```typescript
{ path: 'Accounts/:institutionId', component: Account, canActivate: [authGuard], title: 'Account' }
```

The `institutionId` parameter (GUID) determines which institution's data is displayed.

---

## 2. Institution Navigation

### 2.1 Shell Sidebar Integration

The app shell sidebar dynamically loads the user's institutions and renders them as navigation items under an "Accounts" section.

**Behavior**:
- On app boot, shell fetches `GET api/accounts/institutions`
- Each institution appears as a nav item with name + account count badge
- Active institution is highlighted with accent color
- Clicking navigates to `/Accounts/{institutionId}`
- If user has only one institution, sidebar still shows it (selected by default)
- If user has no institutions/accounts, show "No accounts linked" with CTA to Settings

**Data source**: `GET api/accounts/institutions` (NEW endpoint)

Returns:

```typescript
interface InstitutionSummary {
  id: string;              // GUID
  name: string;            // e.g., "Wells Fargo", "Chase"
  accountCount: number;
  accountTypeCodes: string[]; // e.g., ["CHK", "SAV"]
}
```

### 2.2 Shell Sidebar Changes

| Current | New |
|---|---|
| Hardcoded institution list (Wells Fargo, Schwab, Discover, Chase) | Dynamic list from `GET api/accounts/institutions` |
| Numeric IDs (1-4) | GUID-based `institutionId` |
| Always visible | Visible only when accounts exist |

The shell component fetches institutions on boot via a new `InstitutionClient`. The list is cached — it does not refetch on route changes within the Accounts section.

---

## 3. Layout

Follows the 3-row shared layout pattern defined in `00-Architecture-Shared-Components.md`.

### Row 1 — Account Balance Cards

Summary cards showing balances by account type for the selected institution.

| Card | Value | Notes |
|---|---|---|
| **Checking** | Sum of CHK accounts | Hidden if no checking accounts at this institution |
| **Savings** | Sum of SAV accounts | Hidden if no savings accounts |
| **Investments** | Sum of INV accounts | Hidden if no investment accounts |
| **Credit** | Sum of CRD accounts | Shown as negative balance, hidden if no credit cards |

Card count varies (1-4) based on which account types exist for the institution. Cards that don't apply are omitted (not shown as `$0.00`).

Below the cards, a compact list of individual accounts:

| Account Name | Type | Balance |
|---|---|---|
| Checking ****1234 | CHK | $5,000.00 |
| Savings ****5678 | SAV | $12,000.00 |

Clicking an account filters the transaction table to that account only.

**Data source**: `GET api/accounts/institution/{institutionId}/summary?TimePeriod={period}` (NEW endpoint)

Returns:

```typescript
interface InstitutionSummaryResponse {
  institutionName: string;
  institutionId: string;
  accounts: AccountDetail[];
  accountTypeTotals: Record<string, number>;
  accountTypePreviousTotals: Record<string, number>;
}

interface AccountDetail {
  id: string;
  accountName: string;
  accountTypeCode: string;
  balance: number;
  balanceRecordedDate: string;
}
```

### Row 2 — Charts

#### Left: Spending Over Time (Institution-scoped)

Area chart showing daily spending for accounts within this institution.

- **Type**: Area chart (ECharts) — reuses `SpendActivityChart` shared component
- **Current period**: Solid area fill, primary color
- **Previous period**: Dashed line overlay, muted color
- **X-axis**: Days in period
- **Y-axis**: Daily spend amount
- **Header metrics**: Total spend for period + difference vs previous period

**Data source**: `GET api/Spending/SpendingOverTimeSummary?institutionId={institutionId}` (existing endpoint, extended with `institutionId` param)

#### Right: Spending by Category Donut

Donut chart showing spending breakdown by category for this institution's accounts.

- **Type**: Donut chart (ECharts) — reuses `CategoryPieChart` shared component
- **Center**: Total spend figure
- **Segments**: Spending categories with consistent color mapping
- **Below chart**: Compact table of categories (name, amount, %, transaction count)

**Data source**: `GET api/Spending/SpendingActivityByPeriod?institutionId={}&startDate={}&spendingPeriod={}` (existing endpoint, extended with `institutionId` param)

### Row 3 — Transaction Table

Paginated table of transactions scoped to the selected institution's accounts.

#### Filter Bar

Uses shared `FilterBar` component with these filters enabled:

| Filter | Type | Description |
|---|---|---|
| **Date Range** | Date pickers | Override period selector |
| **Account** | Dropdown | Filter by specific account within institution |
| **Category** | Dropdown | Filter by spending category |
| **Type** | Dropdown | Income/Spend |
| **Search** | Text input | Free-text on description |

**Note**: Amount range filter is NOT shown on this page (less relevant for institution-scoped view).

#### Table Columns

| Column | Sortable | Description |
|---|---|---|
| **Date** | Yes (default, desc) | Transaction date |
| **Description** | Yes | Merchant or description |
| **Category** | Yes | Transaction category |
| **Account** | Yes | Account name |
| **Amount** | Yes | Currency, color-coded |

**Note**: Type column is NOT shown on this page (Account column provides enough context).

#### Pagination

Traditional page numbers, 20 items per page, page size selector.

- **Initial load**: Page 1 data is part of the initial load gate (`loading()`)
- **Page turns**: Show a local loading indicator on the table only (`resource.isLoading()`)

**Data source**: `GET api/transactions?institutionId={}&page={}&pageSize={}&sortField=Date&sortDirection=Descending&...` (existing endpoint, extended with `institutionId` param)

---

## 4. Component Structure

```
features/account/
├── account.ts                        # Main component
├── account.html
├── account.css                       # Empty (Tailwind utility classes)
├── account-page-service.ts           # loading(), error(), computed data signals
└── components/
    ├── account-balance-cards/        # Dynamic card rendering (1-4 cards)
    ├── account-balance-cards-skeleton/
    ├── account-list/                 # Compact account list with click-to-filter
    └── account-list-skeleton/
```

Shared components used:
- `SummaryCard` / `SummaryCardSkeleton` — Row 1 cards (dynamic count)
- `PeriodSelector` — Page header
- `SpendActivityChart` / `SpendActivityChartSkeleton` — Row 2 left
- `CategoryPieChart` / `CategoryPieChartSkeleton` — Row 2 right
- `CategoryBreakdownList` / `CategoryBreakdownListSkeleton` — Row 2 right, below chart
- `FilterBar` — Row 3 filter bar
- `TransactionTable` / `TransactionTableSkeleton` — Row 3 table
- `PaginationControls` — Row 3 pagination

---

## 5. Data Flow

```
AccountPageService
├── Injects: AccountClient, SpendingClient, TransactionsClient
│
├── loading()  → true while any of 4 resources has not returned a value
│   (institutionsResource loaded once on boot, excluded from per-page loading gate)
├── error()    → true if any resource failed before returning a value
│
├── Computed data signals:
│   ├── institutionSummary    → InstitutionSummaryResponse
│   ├── spendingOverTime      → SpendingOverTimeSummary
│   ├── categoryBreakdown     → SpendingByPeriod
│   └── transactions          → ListResult<Transaction> (page 1)
│
└── Exposed resources:
    ├── institutionSummaryResource
    ├── spendingOverTimeResource
    ├── categoryBreakdownResource
    └── transactionsResource  → table checks this for page-turn loading
```

**Note**: The institutions list is fetched by the shell component on boot, not by this page service. The page service reads `institutionId` from the route parameter.

All resources reload when `institutionId` route param changes.

---

## 6. API Changes Required

### 6.1 New Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `GET api/accounts/institutions` | Account shell | Institution list for sidebar |
| `GET api/accounts/institution/{id}/summary?TimePeriod=` | Account | Row 1 cards + account list |

### 6.2 Existing Endpoint Extensions

| Endpoint | Extension |
|---|---|
| `GET api/Spending/SpendingOverTimeSummary` | Add optional `institutionId` query param |
| `GET api/Spending/SpendingActivityByPeriod` | Add optional `institutionId` query param |
| `GET api/transactions` | Add optional `institutionId` query param |

### 6.3 New TypeScript Interfaces

| Type | Location | Description |
|---|---|---|
| `InstitutionSummary` | `shared/api/accounts/institution.ts` | Institution list item |
| `InstitutionSummaryResponse` | `shared/api/accounts/institution.ts` | Institution-scoped account data |
| `AccountDetail` | `shared/api/accounts/institution.ts` | Account detail for institution summary |

### 6.4 Client Changes

New `AccountClient` (or extend existing) with:

```typescript
getInstitutions(): HttpResource<InstitutionSummary[]>
getInstitutionSummary(institutionId: signal<string>, period: signal<TimePeriod>): HttpResource<InstitutionSummaryResponse>
```

`SpendingClient` and `TransactionsClient` extended to accept optional `institutionId` parameter.

---

## 7. Period Selector Behavior

- Defaults to `Monthly`
- Affects all data sources simultaneously
- No custom date range (Transactions page handles custom ranges)

---

## 8. Responsive Behavior

| Viewport | Behavior |
|---|---|
| Desktop (≥1280px) | Shell sidebar visible, main content scrolls |
| Tablet (768px–1279px) | Shell sidebar collapses to icon-only |
| Mobile (<768px) | Shell sidebar becomes slide-out drawer |

---

## 9. Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` for Row 1, chart skeletons for Row 2, `TransactionTableSkeleton` for Row 3.
- **Error**: All components hidden. `ErrorState` with retry button centered on page.

### Subsequent Fetch (Institution Change / Period Change)

- **Error on Row 1 or Row 2 resources**: Current institution's data remains visible. Toast: "Failed to refresh data. Click to retry."
- **Error on Row 3 (table fetch)**: Table shows inline error with retry button. Rest of page unchanged.
- **Sidebar data**: Loaded once on boot. If it fails, sidebar shows cached institution names.

### No Data

- **No accounts for institution**: Cards section hidden. Charts show "No data". Table shows empty message with "Link accounts" CTA → Settings.
- **No transactions for period**: Cards show balances (static). Charts show "No spending data". Table shows empty message.

---

## 10. Future-Proofing Notes

- **Account switching**: Future enhancement — allow user to set a "default institution" loaded on app boot. The shell sidebar should read this from user preferences.
- **Multi-institution comparison**: Future enhancement — side-by-side comparison of spending across institutions. The chart components should support multiple data series.
- **Account linking UI**: Future enhancement — in-app flow to link new accounts. Currently requires Google spreadsheet setup in Settings page.
- **Institution health status**: Future enhancement — show connection status (connected, stale, error) per institution. The `InstitutionSummary` type should include a `lastSyncedDate` or `status` field.
