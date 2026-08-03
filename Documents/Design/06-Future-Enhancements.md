# Future Enhancements — Research & Planned

## OpenAPI Codegen for Client Interfaces

Replace hand-written TypeScript interfaces in `shared/api/` with auto-generated types from the backend OpenAPI spec. API URLs, query parameters, and DTOs should be generated to eliminate manual sync between C# DTOs and TypeScript interfaces. The current `PageService` + `*-client.ts` pattern is the scaffolding for this transition.

**Current state**: `shared/api/` contains manually-maintained TypeScript interfaces (Transaction, Account, SpendingByPeriod, etc.) that mirror backend DTOs. Any backend DTO change requires a manual TypeScript update.

**Goal**: Run codegen against the OpenAPI spec (already exposed at `/openapi` in dev) to generate types, endpoint paths, and query parameter shapes.

## Configurable App Settings API

Move `shared/data/` maps (icons, colors, category types, account types, transaction types) into a backend-driven global app config endpoint so they are configurable at runtime instead of hardcoded in the client.

**Current state**: `shared/data/account-type-map.ts`, `category-map.ts`, `transaction-type-map.ts` are static TypeScript files that mirror backend enums.

**Goal**: Single API call on boot returns the full app configuration (icons, colors, enum displays). Client consumes as a signal or immutable config object.

## Dynamic Account Navigation

Replace hardcoded account nav items in `app.ts` with a new API that returns the authenticated user's distinct institutions.

**Current state**: `app.ts` has a static array of Wells Fargo, Charles Schwab, Discover, and Chase.

**Goal**: New endpoint (e.g., `GET api/accounts/institutions`) that returns distinct institutions for the authenticated user. Shell component fetches on boot and renders nav items dynamically.

## Auth Interceptor Error Handling

Add 401 handling, token refresh, and redirect-to-login flow to `auth.interceptor.ts`.

**Current state**: Interceptor only attaches the Bearer token. No retry, no 401 detection, no redirect on expired tokens.

**Goal**: Interceptor detects 401, attempts token refresh, retries the original request, and redirects to login if refresh fails.

## ProtoBuf Contracts

Research using Protocol Buffers as a shared contract layer between C# and TypeScript to eliminate duplicate interfaces across language boundaries.

**Motivation**: C# and TypeScript both define the same interfaces (Transaction, Account, etc.). ProtoBuf could generate both from a single `.proto` definition.

**Research questions**:
- Tooling for .NET 10 + Angular 21?
- Impact on Wolverine endpoint definitions?
- Does it compose with OpenAPI / Scalar docs?
