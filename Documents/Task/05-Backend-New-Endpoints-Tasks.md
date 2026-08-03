# Backend New Endpoints — Implementation Tasks

Derived from `Documents/Spec/05-Backend-New-Endpoints.md`.

---

## Phase 1 — Domain & Infrastructure Foundations

### T1.1: Create `Institution` entity
- **File**: `src/BasicFinance.Infrastructure/Entities/Institution.cs`
- **Details**:
  - Properties: `InstitutionId` (Guid, PK), `InstitutionCode` (string, required, max 25, e.g. "WF"), `Name` (string, required, max 255), `LogoUrl` (string?, max 500), `Notes` (string?, max 255)
  - `ICollection<Account> Accounts` navigation
  - Implement `IEntity`
  - Parameterized constructor + private parameterless constructor for EF

### T1.2: Refactor `Account` entity
- **File**: `src/BasicFinance.Infrastructure/Entities/Account.cs`
- **Details**:
  - Remove `string Institution` property
  - Add `Guid InstitutionId` + `Institution Institution` navigation with `[ForeignKey]`
  - Update constructor: replace `string institution` param with `Guid institutionId`, assign `InstitutionId = institutionId`

### T1.3: Add Institutions DbSet to `AppDbContext`
- **File**: `src/BasicFinance.Infrastructure/AppDbContext.cs`
- **Details**:
  - Add `public DbSet<Institution> Institutions { get; init; } = null!;`
  - Do NOT add `OnModelCreating` configuration

### T1.4: Add `AddPeriod` extension to `DateTimeExtensions`
- **File**: `src/BasicFinance.Domain/Extensions/DateTimeExtensions.cs`
- **Details**:
  - `public DateTime AddPeriod(TimePeriod timePeriod)` inside the extension block
  - Weekly → `AddDays(7)`, Monthly → `AddMonths(1)`, Quarterly → `AddMonths(3)`, Yearly → `AddYears(1)`
  - Throw `ArgumentOutOfRangeException` on unknown value

### T1.5: Add `AddPeriod` extension to `DateTimeOffsetExtensions`
- **File**: `src/BasicFinance.Domain/Extensions/DateTimeOffsetExtensions.cs`
- **Details**:
  - Same logic as T1.4 but for `DateTimeOffset`

### T1.6: Create `PeriodBoundaryHelper`
- **File**: `src/BasicFinance.Domain/Helpers/PeriodBoundaryHelper.cs`
- **Details**:
  - `GetCurrentAndPreviousPeriod(DateTime now, TimePeriod)` → `(DateTimeRange CurrentPeriod, DateTimeRange PreviousPeriod)`
  - `GetCurrentAndPreviousPeriod(DateTimeOffset now, TimePeriod)` → `(DateTimeOffsetRange CurrentPeriod, DateTimeOffsetRange PreviousPeriod)`
  - `GetExpectedDaysInPeriod(DateTime now, TimePeriod)` → int (7 / daysInMonth / quarterDays / 365 or 366)

---

## Phase 2 — EF Core Migration

### T2.1: Remove existing migration files
- **Files**: `src/BasicFinance.Infrastructure/Migrations/` (all files including snapshot)
- **Details**:
  - Delete all migration files and `AppDbContextModelSnapshot.cs`
  - DB data will be wiped and recreated

### T2.2: Regenerate migration
- **Command**: `dotnet ef migrations add Initial` (run from `src/BasicFinance.Infrastructure`)
- **Details**:
  - Migration should create `Institutions` table, `Accounts.InstitutionId` FK, and all existing tables

---

## Phase 3 — Update Existing Endpoints & DataProcessor

### T3.1: Update `ListAccounts` endpoint
- **File**: `src/BasicFinance.Api/Features/Accounts/ListAccounts.cs`
- **Details**:
  - `Request.Institution` filter: change from `string?` to `string?` matching against `x.Institution.InstitutionCode` (keep parameter name, change query to join on Institution and filter by InstitutionCode)
  - `AccountDto.Institution` projection: change from `x.Institution` to `x.Institution.Name`
  - `SortFieldExpressionSelectors["Institution"]`: change from `x => x.Institution` to `x => x.Institution.Name`
  - Add `.Include(x => x.Institution)` to base query

### T3.2: Update `GetAccountById` endpoint
- **File**: `src/BasicFinance.Api/Features/Accounts/GetAccountById.cs`
- **Details**:
  - DTO projection: change `x.Institution` to `x.Institution.Name`
  - Add `.Include(x => x.Institution)` to query

### T3.3: Update `GetAllAccountAnalytics` endpoint
- **File**: `src/BasicFinance.Api/Features/Accounts/GetAllAccountAnalytics.cs`
- **Details**:
  - `AccountData` projections: change `a.Institution` to `a.Institution.Name`
  - Previous period query: change `h.Account.Institution` to `h.Account.Institution.Name`
  - Add `.Include(a => a.Institution)` to base queries

### T3.4: Update `SyncFinancialDataHandler`
- **File**: `src/BasicFinance.DataProcessor/Handlers/SyncFinancialDataHandler.cs`
- **Details**:
  - `existingAccountsDict` key: change from `new { x.FinancialAccountId, x.Institution }` to `new { x.FinancialAccountId, InstitutionName = x.Institution.Name }`
  - `accountRowDict` key: keep as `new { x.FinancialAccountId, x.Institution }` (still uses string from spreadsheet row)
  - Institution lookup: before creating account, find or create `Institution` by matching `accountRow.Institution` to an existing institution's `Name`, then pass `institution.InstitutionId` to Account constructor
  - Export type dictionaries: change keys from institution name to `InstitutionCode` (e.g., "WF" instead of "Wells Fargo"), or keep name-based lookup but resolve via Institution entity
  - Transaction sync: change `account.Institution` references to `account.Institution.Name`

### T3.5: Fix spending endpoint hardcoded dates + add `RecordedDate` + `TimePeriod` params
- **Files**: `src/BasicFinance.Api/Features/Spending/GetSpendingOverTimeSummary.cs`, `GetSpendingActivityByPeriod.cs`
- **Details for `GetSpendingOverTimeSummary`**:
  - Add `Request` record with `DateTimeOffset? RecordedDate` and `TimePeriod TimePeriod`
  - Replace hardcoded `new DateTime(2025, 11, 25, ...)` with `request.RecordedDate ?? timeProvider.GetUtcNow()`
  - Use `PeriodBoundaryHelper` for period calculations
  - Adapt `BuildCumulativeSpend` to work with variable period lengths (not hardcoded 31 days)
- **Details for `GetSpendingActivityByPeriod`**:
  - Replace hardcoded date with `timeProvider.GetUtcNow()`
  - Replace local `SpendingPeriod` enum with domain `TimePeriod` enum
  - Add `previousPeriod` field to response (same structure as current period)
  - Use `PeriodBoundaryHelper` for period calculations

---

## Phase 4 — New Endpoints

### T4.1: `GET api/accounts/institutions`
- **File**: `src/BasicFinance.Api/Features/Accounts/ListInstitutions.cs`
- **Details**:
  - No query parameters
  - Response: `List<InstitutionDto>` where `InstitutionDto(InstitutionId, Name, AccountCount, AccountTypeCodes)`
  - Query: join `Institutions` → `Accounts` (filtered by `userId` + `IsActive`), group by institution, project DTO
  - `[Authorize]`, `[WolverineGet("api/accounts/institutions")]`
  - XML comments for OpenAPI

### T4.2: `GET api/accounts/institution/{institutionId}/summary?TimePeriod={period}`
- **File**: `src/BasicFinance.Api/Features/Accounts/GetInstitutionSummary.cs`
- **Details**:
  - Path: `institutionId` (Guid), Query: `TimePeriod` (enum, default Monthly)
  - Auth check: user must own at least one active account at this institution → 403 if not
  - Response: `InstitutionSummaryResponse(InstitutionName, InstitutionId, Accounts, AccountTypeTotals, AccountTypePreviousTotals)`
  - `AccountDetailDto(Id, AccountName, AccountTypeCode, Balance, BalanceRecordedDate)`
  - Current period totals from account balances; previous period from `AccountBalanceHistory`
  - Use `PeriodBoundaryHelper` for period ranges

### T4.3: `GET api/transactions/summary?TimePeriod={period}`
- **File**: `src/BasicFinance.Api/Features/Transactions/GetTransactionSummary.cs`
- **Details**:
  - Query: `TimePeriod` (enum, required)
  - Response: `TransactionSummaryResponse(CurrentPeriod, PreviousPeriod)`
  - `TransactionPeriodSummary(TotalCount, TotalSpend, TotalIncome, NetFlow)`
  - Use `PeriodBoundaryHelper` for current + previous period ranges
  - Debit = spend, Credit = income, `NetFlow = TotalIncome - TotalSpend`

### T4.4: `GET api/transactions/dailySummary?TimePeriod={period}`
- **File**: `src/BasicFinance.Api/Features/Transactions/GetDailyTransactionSummary.cs`
- **Details**:
  - Query: `TimePeriod` (enum, required)
  - Response: `DailySummaryResponse(CurrentPeriod, PreviousPeriod)`
  - `DailyTransactionSummary(Date, TotalSpend, TransactionCount)`
  - Group debit transactions by date, fill missing days with `{ Date, 0, 0 }`
  - Use `PeriodBoundaryHelper.GetExpectedDaysInPeriod` to determine expected day count

---

## Phase 5 — Existing Endpoint Extensions

### T5.1: Add `institutionId` filter to `ListTransactions`
- **File**: `src/BasicFinance.Api/Features/Transactions/ListTransactions.cs`
- **Details**:
  - Add `Guid? InstitutionId` to `Request` record
  - In `ApplyFilters`: when provided, query account IDs for that institution + user, filter transactions by account ID list
  - Auth: validate user owns accounts at this institution

### T5.2: Add `institutionId` filter to `GetSpendingOverTimeSummary`
- **File**: `src/BasicFinance.Api/Features/Spending/GetSpendingOverTimeSummary.cs`
- **Details**:
  - Add `Guid? InstitutionId` to `Request` record
  - When provided, filter transactions to accounts belonging to that institution
  - Auth check for institution ownership

### T5.3: Add `institutionId` filter to `GetSpendingActivityByPeriod`
- **File**: `src/BasicFinance.Api/Features/Spending/GetSpendingActivityByPeriod.cs`
- **Details**:
  - Add `Guid? InstitutionId` to `Request` record
  - Same filtering + auth logic as T5.2

---

## Phase 6 — Integration Test Infrastructure

### T6.1: Create `BasicFinance.Api.IntegrationTests` project
- **Location**: `tests/BasicFinance.Api.IntegrationTests/`
- **Details**:
  - xUnit test project, target `net10.0`
  - References: `BasicFinance.Api`, `BasicFinance.Infrastructure`, `BasicFinance.Domain`
  - NuGet: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`, `Testcontainers.RabbitMq`, `Moq`, `Microsoft.EntityFrameworkCore`

### T6.2: Implement `TestContainerFixture`
- **File**: `tests/BasicFinance.Api.IntegrationTests/TestContainerFixture.cs`
- **Details**:
  - `ICollectionFixture<TestContainerFixture>`
  - Static `PostgreSQL` container (`postgres:17-alpine`, user/password `test`, db `basicfinance_test`)
  - Static `RabbitMQ` container (`rabbitmq:4-management`)
  - Start containers in static constructor

### T6.3: Implement `TestApplicationFactory`
- **File**: `tests/BasicFinance.Api.IntegrationTests/TestApplicationFactory.cs`
- **Details**:
  - Inherit `WebApplicationFactory<Program>` from `BasicFinance.Api.Program`
  - Override `ConfigureHostApplicationBuilder`:
    - Replace connection string with TestContainers PostgreSQL
    - Replace Keycloak auth with `TestAuthHandler`
    - Mock `IGoogleUserClient` and `GoogleServiceAccountClient`
    - Replace `TimeProvider` with configurable fake

### T6.4: Implement `TestAuthHandler`
- **File**: `tests/BasicFinance.Api.IntegrationTests/TestAuthHandler.cs`
- **Details**:
  - `AuthenticationHandler<AuthenticationSchemeOptions>`
  - Fixed test user claims: `NameIdentifier = "test-user-id"`, `Name = "Test User"`, `GivenName = "Test"`, `Surname = "User"`, `Email = "test@test.com"`

### T6.5: Create entity factory helpers
- **Location**: `tests/BasicFinance.Api.IntegrationTests/Helpers/`
- **Files**:
  - `InstitutionFactory.cs` — create Institution entities
  - `AccountFactory.cs` — create Account entities (with InstitutionId FK)
  - `TransactionFactory.cs` — create Transaction entities
  - `AccountBalanceHistoryFactory.cs` — create balance history entries
  - `UserGoogleSpreadsheetFactory.cs` — create spreadsheet entities
  - `SeedDataHelper.cs` — convenience methods: `SeedUserWithInstitutionsAndAccounts()`, `SeedTransactionsForPeriod(DateTime, TimePeriod)`, `SeedBalanceHistoryForPeriod(DateTime, TimePeriod)`

---

## Phase 7 — Integration Tests

### T7.1: `ListInstitutionsTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Accounts/ListInstitutionsTests.cs`
- **Tests**:
  - `ListInstitutions_NoAccounts_ReturnsEmptyList`
  - `ListInstitutions_WithMultipleAccounts_ReturnsDistinctInstitutions`
  - `ListInstitutions_IncludesAccountTypeCodes`
  - `ListInstitutions_ExcludesInactiveAccounts`

### T7.2: `GetInstitutionSummaryTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Accounts/GetInstitutionSummaryTests.cs`
- **Tests**:
  - `GetInstitutionSummary_ValidInstitution_ReturnsSummary`
  - `GetInstitutionSummary_OtherUsersInstitution_ReturnsForbidden`
  - `GetInstitutionSummary_NonExistentInstitution_ReturnsForbidden`
  - `GetInstitutionSummary_WithTimePeriod_UsesCorrectPeriodTotals`
  - `GetInstitutionSummary_AccountTypeTotals_AggregatedCorrectly`

### T7.3: `GetTransactionSummaryTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Transactions/GetTransactionSummaryTests.cs`
- **Tests**:
  - `GetTransactionSummary_MonthlyPeriod_ReturnsCorrectTotals`
  - `GetTransactionSummary_CurrentAndPreviousPeriod_BothPopulated`
  - `GetTransactionSummary_NoTransactions_ReturnsZeroTotals`
  - `GetTransactionSummary_ExcludesInactiveTransactions`
  - `GetTransactionSummary_NetFlow_CalculatedAsIncomeMinusSpend`

### T7.4: `GetDailyTransactionSummaryTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Transactions/GetDailyTransactionSummaryTests.cs`
- **Tests**:
  - `GetDailySummary_ReturnsDailyData`
  - `GetDailySummary_MissingDays_ReturnZeroValues`
  - `GetDailySummary_OnlyCountsDebitTransactions`
  - `GetDailySummary_CurrentAndPreviousPeriod_BothPopulated`
  - `GetDailySummary_ExcludesInactiveTransactions`

### T7.5: `ListTransactionsInstitutionFilterTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Transactions/ListTransactionsInstitutionFilterTests.cs`
- **Tests**:
  - `ListTransactions_WithInstitutionId_ReturnsFilteredResults`
  - `ListTransactions_WithOtherUsersInstitution_ReturnsEmpty`
  - `ListTransactions_WithoutInstitutionId_ReturnsAll`

### T7.6: `SpendingEndpointsInstitutionFilterTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Spending/SpendingEndpointsInstitutionFilterTests.cs`
- **Tests**:
  - `SpendingOverTimeSummary_WithInstitutionId_ReturnsFilteredData`
  - `SpendingActivityByPeriod_WithInstitutionId_ReturnsFilteredData`
  - `SpendingActivityByPeriod_ReturnsPreviousPeriodData`

### T7.7: `SpendingEndpointsTimePeriodTests`
- **File**: `tests/BasicFinance.Api.IntegrationTests/Features/Spending/SpendingEndpointsTimePeriodTests.cs`
- **Tests**:
  - `SpendingOverTimeSummary_WithRecordedDate_UsesProvidedDate`
  - `SpendingOverTimeSummary_WithTimePeriod_UsesCorrectPeriod`

---

## Phase 8 — Unit Tests

### T8.1: `AddPeriod` tests for `DateTimeExtensions`
- **File**: `tests/BasicFinance.Domain.UnitTests/Extensions/DateTimeExtensionsTests.cs` (add to existing)
- **Tests**:
  - `AddPeriod_Weekly_AddsSevenDays`
  - `AddPeriod_Monthly_AddsOneMonth`
  - `AddPeriod_Quarterly_AddsThreeMonths`
  - `AddPeriod_Yearly_AddsOneYear`

### T8.2: `AddPeriod` tests for `DateTimeOffsetExtensions`
- **File**: `tests/BasicFinance.Domain.UnitTests/Extensions/DateTimeOffsetExtensionsTests.cs` (add to existing)
- **Tests**:
  - `AddPeriod_Weekly_AddsSevenDays`
  - `AddPeriod_Monthly_AddsOneMonth`
  - `AddPeriod_Quarterly_AddsThreeMonths`
  - `AddPeriod_Yearly_AddsOneYear`

### T8.3: `PeriodBoundaryHelper` tests
- **File**: `tests/BasicFinance.Domain.UnitTests/Helpers/PeriodBoundaryHelperTests.cs`
- **Tests**:
  - `GetCurrentAndPreviousPeriod_Monthly_ReturnsCorrectRanges`
  - `GetCurrentAndPreviousPeriod_Weekly_ReturnsCorrectRanges`
  - `GetCurrentAndPreviousPeriod_PreviousPeriod_OffsetMinusOne`
  - `GetExpectedDaysInPeriod_Weekly_ReturnsSeven`
  - `GetExpectedDaysInPeriod_Monthly_February_LeapYear_ReturnsTwentyNine`
  - `GetExpectedDaysInPeriod_Monthly_February_NonLeapYear_ReturnsTwentyEight`
  - `GetExpectedDaysInPeriod_Yearly_LeapYear_ReturnsThreeSixtySix`
  - `GetExpectedDaysInPeriod_Yearly_NonLeapYear_ReturnsThreeSixtyFive`

### T8.4: `Institution` entity tests
- **File**: `tests/BasicFinance.Infrastructure.UnitTests/Entities/InstitutionTests.cs`
- **Tests**:
  - `Constructor_ValidParameters_CreatesSuccessfully`
  - `Constructor_GeneratesNewGuid`
  - `IsActive_DefaultsToTrue`
  - `SystemCreatedDate_DefaultsToUtcNow`

---

## Dependency Graph

```
T1.1 → T1.2 → T1.3 → T2.1 → T2.2
T1.4, T1.5, T1.6 (parallel with T1.1-T1.3)

T2.2 → T3.1, T3.2, T3.3, T3.4, T3.5

T3.5 → T4.1, T4.2, T4.3, T4.4
T3.1, T3.2, T3.3 → T4.1, T4.2

T4.1-T4.4 → T5.1, T5.2, T5.3

T2.2 → T6.1 → T6.2, T6.3, T6.4, T6.5
T6.1-T6.5 → T7.1-T7.7

T1.4, T1.5, T1.6 → T8.1, T8.2, T8.3
T1.1 → T8.4
```
