# BasicFinance — Agent Instructions

## Architecture

.NET 10 Aspire cloud-native app with an Angular 21 SPA frontend. All services are orchestrated through `BasicFinance.AppHost`.

| Project                              | Role                                                                                                                     |
| ------------------------------------ | ------------------------------------------------------------------------------------------------------------------------ |
| `BasicFinance.AppHost`               | Aspire orchestrator. Entry point is `AppHost.cs` (not `Program.cs`). Provisions PostgreSQL (x2), Keycloak, and RabbitMQ. |
| `BasicFinance.Api`                   | ASP.NET Core Web API. Wolverine + RabbitMQ for messaging. Keycloak OIDC auth.                                            |
| `BasicFinance.DataProcessor`         | Background web worker. Wolverine + RabbitMQ consumers.                                                                   |
| `BasicFinance.MigrationWorker`       | EF Core migration runner. Executes on startup via AppHost.                                                               |
| `BasicFinance.Domain`                | Shared domain models. No infrastructure deps.                                                                            |
| `BasicFinance.Infrastructure`        | EF Core DbContext, PostgreSQL provider, Google Sheets/Drive SDK. Migrations live here.                                   |
| `BasicFinance.ServiceDefaults`       | Aspire shared project: OpenTelemetry, health checks, resilience, service discovery.                                      |
| `BasicFinance.SharedServiceDefaults` | Service discovery name constants (`ServiceDiscoveryNames.cs`).                                                           |
| `BasicFinance.Client`                | Angular 21 SPA. OAuth2/OIDC via `angular-oauth2-oidc`. spartan-ng UI library. Tailwind CSS v4.                           |

## Developer Commands

### Full stack (Aspire)

```powershell
dotnet run --project src/BasicFinance.AppHost
```

AppHost auto-restores npm packages per its MSBuild target. Requires secret params (DB creds, Keycloak creds, Google service account path) set via `dotnet user-secrets` or Aspire dash.

### .NET only

```powershell
dotnet build
dotnet build --no-incremental    # clean rebuild
```

### Root tooling (Vite+)

Lint, format, and type checks are owned by Vite+ at the repo root. Git hooks: `pre-commit` runs `vp staged`; `pre-push` runs .NET unit tests and client unit tests when the changed paths touch those areas.

```powershell
vp install                       # install dependencies across all workspaces
vp check                         # format check + lint + type check
vp fmt                           # write formatting
```

### Angular client (standalone)

```powershell
cd src/BasicFinance.Client
npm install
npm run start-standalone         # dev server on :4200
npm run build                    # production build
npm run test                     # watch mode (ng test)
npm run test:unit                # headless unit tests with coverage
```

## Testing

Unit and integration tests are separated into distinct projects. Run from solution root:

```powershell
dotnet test tests/BasicFinance.Domain.UnitTests
dotnet test tests/BasicFinance.Infrastructure.UnitTests
dotnet test  # runs all test projects
```

| Test Project                                  | Type        | What it tests                                                    |
| --------------------------------------------- | ----------- | ---------------------------------------------------------------- |
| `BasicFinance.Domain.UnitTests`               | Unit        | Domain models, extensions (no infra deps)                        |
| `BasicFinance.Infrastructure.UnitTests`       | Unit        | EF Core query extensions (no DB)                                 |
| `BasicFinance.Api.IntegrationTests`           | Integration | API vertical slices via `WebApplicationFactory` + TestContainers |
| `BasicFinance.DataProcessor.IntegrationTests` | Integration | Wolverine message handlers via TestContainers                    |

### Unit Tests

Fast, isolated. Mock all infrastructure (DB, network, file system). See `tests/AGENTS.md` for conventions.

### Integration Tests

Test the full pipeline: endpoint → middleware → DbContext → PostgreSQL. Use TestContainers for real infrastructure (PostgreSQL, RabbitMQ, Keycloak). External services (Google SDK) are mocked.

### Test Project Naming

- Unit test projects: `<SourceProject>.UnitTests`
- Integration test projects: `<SourceProject>.IntegrationTests`
- Test files mirror the source project folder structure.

## Environment Gotchas

- **Client config at runtime**: The Angular app fetches `/environment-config.json` on boot. In dev, `proxy.conf.ts` intercepts this and serves from `environment-config-data.ts`. In prod, `postbuild` script writes the JSON to `dist/`.
- **Proxy target**: The Angular proxy reads `services__api__https__0` or `services__api__http__0` env vars (Aspire convention). Standalone dev mode has no proxy — API calls go direct.
- **OAuth issuer**: Hardcoded to `https://localhost:8080/realms/basic-hub` in `src/BasicFinance.Client/src/app/core/auth/auth.config.ts`.
- **Google credentials**: `GOOGLE_APPLICATION_CREDENTIALS` env var must point to a service account JSON file for both Api and DataProcessor.
- **EF Core migrations**: Located in `src/BasicFinance.Infrastructure/Migrations/`. Scaffold from that project directory.

## Conventions

### C#

- `.editorconfig` enforces code style at build (`EnforceCodeStyleInBuild=true`). File-scoped namespaces, `var` preferred, expression-bodied properties, braces required.
- NuGet versions centralized in `Directory.Packages.props` (CPM enabled, transitive pinning on).
- Solution file is `.slnx` format (Visual Studio 2022+).
- StyleCop and SonarAnalyzer run as build analyzers.
- API routes: kebab-case segments, no trailing slashes, camelCase path params (see `DevDocuments/Task/openapi-heyapi-client.md`).

### Angular

- Component selectors: `app-kebab-case` (elements), `appCamelCase` (directives).
- UI components live in `libs/ui` (spartan-ng helm layer). Import per module as `@spartan-ng/helm/<module>` (tsconfig paths map to `libs/ui/<module>/src/index.ts`). App code uses the helm layer only; `@spartan-ng/brain` and `@angular/cdk` are internal to `libs/ui` (enforcement tracked in `DevDocuments/Task/spartan-lint-enforcement.md`).
- Auth guard (`authGuard`) protects all routes. OAuth initialized via `provideAppInitializer`.
- Feature modules follow `features/<domain>/` structure; shared data-access services live in `core/data-access/` as `*-client.ts` (or generated OpenAPI client wrappers, see `DevDocuments/Task/openapi-heyapi-client.md`). Never hand-edit `core/api/generated/` — regenerate via `npm run gen:api` from `contracts/openapi.json`.
- `core/api/generated/` imports are confined to `core/`: feature code only imports the data-access facades, which may re-export (and alias) generated types. `openapi-ts.config.ts` is pinned (`responseStyle: 'fields'`, `throwOnError: true`, no extra plugins) so `gen:api` must produce a clean no-diff against committed output.
- Tests use Vitest (`@angular/build:unit-test`). Spec files colocated as `*.spec.ts`; run headless with `npm run test:unit`.
- All component CSS files are intentionally left empty. Styling is applied via Tailwind CSS v4 utility classes inline in templates.

## Documentation

Project documentation lives in `DevDocuments/` organized in three tiers:

| Folder    | Purpose                                                                                |
| --------- | -------------------------------------------------------------------------------------- |
| `Design/` | Loose, exploratory ideas. Hazy or unclear concepts that need refinement.               |
| `Spec/`   | Refined specifications produced by research, analysis, and review of design documents. |
| `Task/`   | Concrete, actionable implementation steps derived from specifications.                 |

<!--VITE PLUS START-->

# Using Vite+, the Unified Toolchain for the Web

This project is using Vite+, a unified toolchain built on top of Vite, Rolldown, Vitest, tsdown, Oxlint, Oxfmt, and Vite Task. Vite+ wraps runtime management, package management, and frontend tooling in a single global CLI called `vp`. Vite+ is distinct from Vite, and it invokes Vite through `vp dev` and `vp build`. Run `vp help` to print a list of commands and `vp <command> --help` for information about a specific command.

Docs are local at `node_modules/vite-plus/docs` or online at https://viteplus.dev/guide/.

## Built-in Commands vs Scripts

`vp <name>` runs a built-in command. `vp run <name>` runs a `package.json` script or a `vite.config.ts` task. Scripts cannot overwrite built-ins, so `vp dev` and `vp run dev` may do different things. Check `package.json` and `vite.config.ts` first, and run `vp run <name>` when the project defines a script or task with that name.

## Running Scripts

Run TypeScript and JavaScript files with `vpx <file>`, for example `vpx ./scripts/seed.ts --dry-run`. It handles enums, decorators, JSX, and tsconfig `paths`, so do not add `tsx` or `ts-node` to run scripts. `vpx --watch <file>` reruns the file when a file it uses changes, in place of `tsx watch`. `vp node <file>` runs plain Node.js.

## Tool Versions

Run `vp toolchain` to show versions and relationships in the active Vite+
release. Add a tool name to select part of the graph. For example, run
`vp toolchain vite`. Use `--global` to ignore the local `vite-plus` package. Use
`vp why <package>` to show the package-manager dependency graph.

## Review Checklist

- [ ] Run `vp install` after pulling remote changes and before getting started.
- [ ] Run `vp check` and `vp test` to format, lint, type check and test changes.
- [ ] Check if there are `vite.config.ts` tasks or `package.json` scripts necessary for validation, run via `vp run <script>`.
- [ ] If setup, runtime, or package-manager behavior looks wrong, run `vp env doctor` and include its output when asking for help.

<!--VITE PLUS END-->
