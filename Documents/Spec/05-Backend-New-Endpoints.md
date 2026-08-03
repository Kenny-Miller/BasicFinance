# Backend New Endpoints Specification

## Status: Approved

## Purpose

Defines all new API endpoints, endpoint extensions, and the new `Institution` entity required to support the Transactions page summary cards, Spending page enhancements, and the Account page.

---

## 1. Institution Entity

### 1.1 Problem

The `Account` entity currently stores `Institution` as a `string` property (e.g., "Wells Fargo", "Chase"). This creates issues:

- No referential integrity — same institution can be spelled differently
- No efficient DISTINCT query for institution list
- No institution-level analytics without string grouping
- URL route uses magic numbers (1-4) instead of stable IDs

### 1.2 Solution

Introduce a first-class `Institution` entity. Accounts reference institutions by foreign key.

```csharp
public class Institution : IEntity
{
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = default!;
    public string? LogoUrl { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}
```

### 1.3 Account Entity Changes

```diff
- public string Institution { get; set; } = default!;
+ public Guid InstitutionId { get; set; }
+ public Institution Institution { get; set; } = default!;
```

### 1.4 Migration Strategy

**Out of scope for this spec.** The migration approach is:

1. Read distinct institution names from existing `Account.Institution` strings (scoped to `userId`)
2. Create `Institution` entities for each distinct name
3. Update `Account.InstitutionId` FK
4. Drop `Account.Institution` string column

This should be handled as a separate EF Core migration with a seed/data migration step.

---

## 2. New Endpoints

### 2.1 `GET api/accounts/institutions`

Returns distinct institutions for the authenticated user.

**Query Parameters**: None

**Response**: `IEnumerable<InstitutionDto>`

```csharp
public record InstitutionDto(
    Guid InstitutionId,
    string Name,
    int AccountCount,
    IEnumerable<string> AccountTypeCodes
);
```

**Implementation**:

```csharp
from institution in context.Institutions
join account in context.Accounts
    on institution.InstitutionId equals account.InstitutionId
where account.UserId == userId && account.IsActive
group account by institution into g
select new InstitutionDto(
    InstitutionId: g.Key.InstitutionId,
    Name: g.Key.Name,
    AccountCount: g.Count(),
    AccountTypeCodes: g.Select(a => a.AccountType.AccountTypeCode).Distinct()
)
```

**Notes**:
- Scoped to authenticated user
- Only includes active accounts
- Returned on shell boot — result is client-cached

---

### 2.2 `GET api/accounts/institution/{institutionId}/summary?TimePeriod={period}`

Returns account-level summary for a specific institution.

**Path Parameters**:
- `institutionId` (Guid, required)

**Query Parameters**:
- `TimePeriod` (enum: Weekly/Monthly/Quarterly/Yearly, default: Monthly)

**Response**: `InstitutionSummaryResponse`

```csharp
public record InstitutionSummaryResponse(
    string InstitutionName,
    Guid InstitutionId,
    IEnumerable<AccountDetailDto> Accounts,
    Dictionary<string, decimal> AccountTypeTotals,
    Dictionary<string, decimal> AccountTypePreviousTotals
);

public record AccountDetailDto(
    Guid Id,
    string AccountName,
    string AccountTypeCode,
    decimal Balance,
    DateTime BalanceRecordedDate
);
```

**Implementation**:

1. Validate `institutionId` belongs to authenticated user (403 if not)
2. Get all active accounts for this institution + user
3. Calculate current period totals by account type
4. Calculate previous period totals by account type (from `AccountBalanceHistory`)
5. Return account details + type totals

**Notes**:
- Authorization check required — user must own at least one account at this institution
- Previous period totals use the same time period calculation as `GetAllAccountAnalytics` (e.g., if current = Aug, previous = Jul)
- `Balance` comes from the latest `AccountBalanceHistory` record per account, or falls back to `Account.Balance` if no history exists

---

### 2.3 `GET api/transactions/summary?TimePeriod={period}`

Returns transaction summary statistics for the current and previous period.

**Query Parameters**:
- `TimePeriod` (enum: Weekly/Monthly/Quarterly/Yearly, required)

**Response**: `TransactionSummaryResponse`

```csharp
public record TransactionSummaryResponse(
    TransactionPeriodSummary CurrentPeriod,
    TransactionPeriodSummary PreviousPeriod
);

public record TransactionPeriodSummary(
    int TotalCount,
    decimal TotalSpend,
    decimal TotalIncome,
    decimal NetFlow
);
```

**Implementation**:

```csharp
// For each period (current + previous):
var startDate = now.ToStartOfPeriod(timePeriod);
var endDate = startDate.AddPeriod(timePeriod);

var transactions = context.Transactions
    .Include(t => t.TransactionType)
    .Where(t => t.UserId == userId
        && t.IsActive
        && t.Date >= startDate
        && t.Date < endDate)
    .ToList();

return new TransactionPeriodSummary(
    TotalCount: transactions.Count,
    TotalSpend: transactions.Where(t => t.TransactionType.TransactionTypeCode == "Debit")
        .Sum(t => Math.Abs(t.Amount)),
    TotalIncome: transactions.Where(t => t.TransactionType.TransactionTypeCode == "Credit")
        .Sum(t => t.Amount),
    NetFlow: 0m // calculated as income - spend
);
```

**Notes**:
- Uses existing `DateTimeExtensions.ToStartOfPeriod()` and period range methods
- "Spend" = Debit transactions, "Income" = Credit transactions
- `NetFlow = TotalIncome - TotalSpend`

---

### 2.4 `GET api/transactions/dailySummary?TimePeriod={period}`

Returns daily transaction summary for the current and previous period.

**Query Parameters**:
- `TimePeriod` (enum: Weekly/Monthly/Quarterly/Yearly, required)

**Response**: `DailySummaryResponse`

```csharp
public record DailySummaryResponse(
    IEnumerable<DailyTransactionSummary> CurrentPeriod,
    IEnumerable<DailyTransactionSummary> PreviousPeriod
);

public record DailyTransactionSummary(
    string Date,
    decimal TotalSpend,
    int TransactionCount
);
```

**Implementation**:

```csharp
var startDate = now.ToStartOfPeriod(timePeriod);
var endDate = startDate.AddPeriod(timePeriod);

var dailySummaries = context.Transactions
    .Where(t => t.UserId == userId
        && t.IsActive
        && t.TransactionType.TransactionTypeCode == "Debit"
        && t.Date >= startDate
        && t.Date < endDate)
    .GroupBy(t => t.Date.Date)
    .Select(g => new DailyTransactionSummary(
        Date: g.Key.ToString("yyyy-MM-dd"),
        TotalSpend: g.Sum(t => Math.Abs(t.Amount)),
        TransactionCount: g.Count()
    ))
    .OrderBy(d => d.Date)
    .ToList();
```

**Notes**:
- Only Debit (spend) transactions are counted
- Returns data for each day in the period (days with no transactions should return `{ Date, 0, 0 }`)
- Both current and previous period data returned in single response

---

## 3. Existing Endpoint Extensions

### 3.1 Spending Endpoints — Institution Filtering

Both spending endpoints need an optional `institutionId` parameter to scope results to a specific institution.

#### `GET api/Spending/SpendingOverTimeSummary`

**New parameter**:
- `institutionId` (Guid?, optional)

**Changes**:
- When `institutionId` is provided, filter transactions to accounts belonging to that institution
- Authorization: validate user owns at least one account at this institution

```csharp
// Additional filter when institutionId is provided:
if (institutionId.HasValue)
{
    var institutionAccountIds = context.Accounts
        .Where(a => a.InstitutionId == institutionId.Value
            && a.UserId == userId
            && a.IsActive)
        .Select(a => a.AccountId)
        .ToList();

    query = query.Where(t => institutionAccountIds.Contains(t.AccountId));
}
```

#### `GET api/Spending/SpendingActivityByPeriod`

**New parameter**:
- `institutionId` (Guid?, optional)

**Changes**: Same filtering logic as above.

---

### 3.2 Transactions Endpoint — Institution Filtering

#### `GET api/transactions`

**New parameter**:
- `institutionId` (Guid?, optional)

**Changes**: When provided, filter to accounts belonging to that institution.

```csharp
if (institutionId.HasValue)
{
    var institutionAccountIds = context.Accounts
        .Where(a => a.InstitutionId == institutionId.Value
            && a.UserId == userId
            && a.IsActive)
        .Select(a => a.AccountId)
        .ToList();

    query = query.Where(t => institutionAccountIds.Contains(t.AccountId));
}
```

---

## 4. TimeProvider Fix

### 4.1 Problem

Both spending endpoints hardcode `new DateTime(2025, 11, 25, 13, 26, 30, ...)` instead of using the injected `TimeProvider`. This means the endpoints always query the same date range regardless of when they're called.

### 4.2 Fix

Replace hardcoded date with `timeProvider.GetUtcNow()` (or `timeProvider.GetLocalNow()`):

```csharp
// Before
var now = new DateTime(2025, 11, 25, 13, 26, 30, DateTimeKind.Utc);

// After
var now = _timeProvider.GetUtcNow().UtcDateTime;
```

Both `GetSpendingOverTimeSummary` and `GetSpendingActivityByPeriod` need this fix.

---

## 5. DTO Consistency

### 5.1 Response Type Naming

New response types follow the existing naming convention:

| Backend C# Record | Frontend TypeScript Interface |
|---|---|
| `TransactionSummaryResponse` | `TransactionSummaryResponse` |
| `TransactionPeriodSummary` | `TransactionPeriodSummary` |
| `DailySummaryResponse` | `DailySummaryResponse` |
| `DailyTransactionSummary` | `DailyTransactionSummary` |
| `InstitutionDto` | `InstitutionSummary` |
| `InstitutionSummaryResponse` | `InstitutionSummaryResponse` |
| `AccountDetailDto` | `AccountDetail` |

### 5.2 Existing DTOs Affected

No changes to existing DTOs. The `TransactionDto` and `AccountDto` remain unchanged. The spending response types (`SpendingByPeriod`, `SpendingOverTimeSummary`) remain unchanged — they gain an optional filter parameter, not a structural change.

---

## 6. Authorization

All endpoints scope data to the authenticated user via `ClaimTypes.NameIdentifier`. The new institution-scoped endpoints add an additional check:

```csharp
var ownsAccount = await context.Accounts
    .AnyAsync(a => a.InstitutionId == institutionId
        && a.UserId == userId
        && a.IsActive);

if (!ownsAccount)
    return Results.Forbidden("Access denied.");
```

This prevents users from accessing another user's institution data by guessing the GUID.

---

## 7. Future-Proofing Notes

- **OpenAPI codegen**: All new endpoints should be documented with XML comments for auto-generated OpenAPI descriptions. Query parameters should have `FromQuery` attributes with descriptions.
- **ProtoBuf contracts**: The new DTOs (`TransactionSummaryResponse`, `DailySummaryResponse`, `InstitutionSummaryResponse`) are candidates for ProtoBuf generation if that path is pursued.
- **Institution health**: The `Institution` entity includes `Notes` and `LogoUrl` fields for future enhancements (connection status, branding).
- **Batch operations**: Future enhancement — bulk category reassignment. A new `POST api/transactions/bulkUpdate` endpoint can be added without affecting existing endpoints.
- **Caching**: The institutions list changes infrequently. Consider response caching (`[ResponseCache]`) for `GET api/accounts/institutions`.
