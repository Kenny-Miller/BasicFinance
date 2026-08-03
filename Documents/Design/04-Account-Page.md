# Account Page Design

## Purpose

The Account page provides a per-institution view of all associated accounts, their balances, spending patterns, and transaction history. Users navigate between institutions via a sidebar navigation.

## Route

```typescript
{ path: 'Accounts/:institutionId', component: Account, canActivate: [authGuard], title: 'Account' }
```

The `institutionId` parameter determines which institution's data is displayed. A sidebar lists all institutions for the user, with the active one highlighted.

## Layout

### Sidebar Navigation

Left sidebar (collapsible on mobile) showing all institutions for the user:

- **Header**: "Institutions" label
- **List**: One item per institution, showing institution name + account count
- **Active state**: Highlighted with accent color
- **Click**: Updates route param, reloads all data for selected institution
- **Mobile**: Drawer/slide-out panel triggered by a hamburger button

**Data source**: `GET api/accounts/institutions` (new endpoint needed)

Returns:

```typescript
interface InstitutionSummary {
  id: string;
  name: string;
  accountCount: number;
  accountTypeCodes: string[]; // e.g., ["CHK", "SAV"]
}
```

### Main Content Area

#### Row 1 — Account Balance Cards

Summary cards showing balances by account type for the selected institution.

| Card | Value | Notes |
|---|---|---|
| **Checking** | Sum of CHK accounts | Hidden if no checking accounts |
| **Savings** | Sum of SAV accounts | Hidden if no savings accounts |
| **Investments** | Sum of INV accounts | Hidden if no investment accounts |
| **Credit** | Sum of CRD accounts | Shown as negative balance, hidden if no credit cards |

Card count varies (2-4) based on which account types exist for the institution. Cards that don't apply are omitted (not shown as `$0.00`).

Below the cards, a compact list of individual accounts:

| Account Name | Type | Balance |
|---|---|---|
| Checking ****1234 | CHK | $5,000.00 |
| Savings ****5678 | SAV | $12,000.00 |

**Data source**: `GET api/accounts/institution/{institutionId}/summary?TimePeriod={period}` (new endpoint)

Returns:

```typescript
interface InstitutionSummaryResponse {
  institutionName: string;
  accounts: AccountDto[];
  accountTypeTotals: Record<string, number>;
  totalSpend: number;
  totalIncome: number;
}
```

#### Row 2 — Charts

##### Left: Spending Over Time (Institution-scoped)

Line chart showing daily spending for accounts within this institution.

- **Type**: Area chart (ECharts)
- **Current period**: Solid area fill, primary color
- **Previous period**: Dashed line overlay, muted color
- **X-axis**: Days in period
- **Y-axis**: Daily spend amount
- **Header metrics**: Total spend for period + difference vs previous period

**Data source**: `GET api/Spending/SpendingOverTimeSummary?institutionId={institutionId}` (extend existing endpoint)

##### Right: Spending by Category Donut

Donut chart showing spending breakdown by category for this institution's accounts.

- **Type**: Donut chart (ECharts)
- **Center**: Total spend figure
- **Segments**: Spending categories with consistent color mapping
- **Below chart**: Compact table of categories (name, amount, %, transaction count)

**Data source**: `GET api/Spending/SpendingActivityByPeriod?institutionId={}&startDate={}&spendingPeriod={}` (extend existing endpoint)

#### Row 3 — Transaction Table

Paginated table of transactions scoped to the selected institution's accounts.

##### Filter Bar

| Filter | Type | Description |
|---|---|---|
| **Date Range** | Date pickers | Override period selector |
| **Account** | Dropdown | Filter by specific account within institution |
| **Category** | Dropdown | Filter by spending category |
| **Type** | Dropdown | Income/Spend/Transfer |
| **Search** | Text input | Free-text on description |

##### Table Columns

| Column | Sortable | Description |
|---|---|---|
| **Date** | Yes (default, desc) | Transaction date |
| **Description** | Yes | Merchant or description |
| **Category** | Yes | Transaction category |
| **Account** | Yes | Account name |
| **Amount** | Yes | Currency, color-coded |

##### Pagination

Traditional page numbers, 20 items per page, page size selector.

- **Initial load**: Page 1 data is part of the initial load gate (`loading()`)
- **Page turns**: Show a local loading indicator on the table only (`resource.isLoading()`)

**Data source**: `GET api/transactions?page={}&pageSize={}&sortField=Date&sortDirection=Descending&institutionId={}&...`

## Period Selector Behavior

- Defaults to `Monthly`
- Affects all data sources simultaneously
- No custom date range (Transactions page handles custom ranges)

## Component Structure

```
features/account/
├── account.ts                        # Main component
├── account.html
├── account.css
├── account-page-service.ts           # loading(), error(), computed data signals
└── components/
    ├── institution-sidebar/
    ├── institution-sidebar-skeleton/
    ├── account-list/
    ├── account-list-skeleton/
    ├── spending-trend-chart/         (can reuse from Spending page)
    ├── category-breakdown-chart/     (can reuse from Spending page)
    ├── category-breakdown-table/     (can reuse from Spending page)
    └── transaction-table/            (can reuse from Transactions page)
        transaction-table-skeleton/
```

## Data Flow

```
AccountPageService
├── Injects: AccountClient, SpendingClient, TransactionsClient (from core/api/)
│
├── loading()  → true while any of the 5 resources has not returned a value
├── error()    → true if any resource failed before returning a value
│
├── Computed data signals:
│   ├── institutions          → InstitutionSummary[]
│   ├── institutionSummary    → InstitutionSummaryResponse
│   ├── spendingOverTime      → SpendingOverTimeSummary
│   ├── categoryBreakdown     → SpendingByPeriod
│   └── transactions          → ListResult<Transaction> (page 1)
│
└── Exposed resources:
    ├── institutionsResource
    ├── institutionSummaryResource
    ├── spendingOverTimeResource
    ├── categoryBreakdownResource
    └── transactionsResource  → table checks this for page-turn loading
```

### Page Service

```typescript
@Injectable()
export class AccountPageService {
  private readonly accountClient = inject(AccountClient);
  private readonly spendingClient = inject(SpendingClient);
  private readonly transactionsClient = inject(TransactionsClient);

  // Raw resources — all 5 are part of initial load
  readonly institutionsResource = httpResource<InstitutionSummary[]>(...);
  readonly institutionSummaryResource = httpResource<InstitutionSummaryResponse>(...);
  readonly spendingOverTimeResource = httpResource<SpendingOverTimeSummary>(...);
  readonly categoryBreakdownResource = httpResource<SpendingByPeriod>(...);
  readonly transactionsResource = httpResource<ListResult<Transaction>>(...);

  // Initial load gate — includes transactions page 1
  readonly loading = computed(() =>
    !this.institutionsResource.hasValue() ||
    !this.institutionSummaryResource.hasValue() ||
    !this.spendingOverTimeResource.hasValue() ||
    !this.categoryBreakdownResource.hasValue() ||
    !this.transactionsResource.hasValue(),
  );

  // Initial load error
  readonly error = computed(() =>
    (this.institutionsResource.error() !== null && !this.institutionsResource.hasValue()) ||
    (this.institutionSummaryResource.error() !== null && !this.institutionSummaryResource.hasValue()) ||
    (this.spendingOverTimeResource.error() !== null && !this.spendingOverTimeResource.hasValue()) ||
    (this.categoryBreakdownResource.error() !== null && !this.categoryBreakdownResource.hasValue()) ||
    (this.transactionsResource.error() !== null && !this.transactionsResource.hasValue()),
  );

  // Transformed data signals
  readonly institutions = computed(() => this.institutionsResource.value() ?? []);
  readonly institutionSummary = computed(() => this.institutionSummaryResource.value() ?? null);
  readonly spendingOverTime = computed(() => this.spendingOverTimeResource.value() ?? null);
  readonly categoryBreakdown = computed(() => this.categoryBreakdownResource.value() ?? null);
  readonly transactions = computed(() => this.transactionsResource.value() ?? null);
}
```

Sidebar data (`institutionsResource`) loads once on page init. All other resources reload when `institutionId` route param changes.

## Design Considerations

### Institution Selection

- If user has only one institution, sidebar still shows (with that one institution selected)
- If user has no institutions/accounts, show a placeholder: "No accounts linked yet" with a CTA to Settings

### Responsive Behavior

- **Desktop**: Sidebar fixed on left, main content scrolls
- **Tablet**: Sidebar collapses to icon-only mode
- **Mobile**: Sidebar becomes a slide-out drawer

### Account List Detail

The account list below Row 1 cards should be compact but informative:
- Account name/mask number
- Account type icon
- Current balance
- Clicking an account filters the transaction table to that account only

## Empty/Error States

### Initial Load

- **Loading**: Full skeleton layout — `SummaryCardSkeleton` for Row 1, chart skeletons for Row 2, `TransactionTableSkeleton` for Row 3, `InstitutionSidebarSkeleton` for sidebar.
- **Error**: All components hidden. Generic error message with retry button centered on page.

### Subsequent Fetch (Institution Change / Period Change)

- **Error on Row 1 or Row 2 resources**: Current data remains visible. Toast notification appears: "Failed to refresh data. Click to retry."
- **Error on Row 3 (table fetch)**: Table shows inline error with retry button. Rest of page unchanged.
- **Sidebar data**: Loaded once. If it fails, sidebar still shows cached institution names from the route.

### No Data

- **No accounts for institution**: Cards section hidden. Charts show "No data". Table shows empty message with "Link accounts" CTA.
- **No transactions for period**: Cards show balances (static). Charts show "No spending data". Table shows empty message.
