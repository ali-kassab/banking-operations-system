# Session handoff

## Session: 2026-11-22 — initial build (v1.0.0)

### Accomplished
- Reviewed public GitHub banking projects for structure ideas (see `docs/project-overview.md`); no code copied.
- Built the complete backend (Domain, Application, Infrastructure, API) on .NET 10 LTS with EF Core + SQL Server.
- Wrote 36 xUnit tests for the money rules, transfers, concurrency, account/customer creation and authentication.
- Built the Angular 22 client with all required pages.
- Verified end-to-end against SQL Server LocalDB (API smoke script + manual browser run).
- Wrote README, architecture/overview docs and these handoff files.
- Created the public GitHub repository and pushed `main`.

### Important decisions
- **.NET 10 (LTS)** instead of .NET 11 (STS); all Microsoft packages pinned to 10.0.15.
- **No service interfaces, no generic repository.** Application defines small, entity-specific repository interfaces;
  Infrastructure implements them.
- **Transfers** use `IUnitOfWork.ExecuteInTransactionAsync` (explicit DB transaction).
- **`Balance` is an EF concurrency token** → stale concurrent updates fail with HTTP 409.
- **Check constraints** in the DB (`Balance >= 0`, `Amount > 0`) back up the domain rules.
- **Admin-only** action: opening bank accounts. Everything else is available to both roles.
- **Tests use in-memory SQLite** (not EF InMemory) so transactions and constraints are real.
- **Dates**: stored as UTC; a value converter marks them UTC on read so JSON ends with `Z`.
- **Dev-only secrets**: `Jwt:Key` is empty in `appsettings.json`; a labelled dev key lives in
  `appsettings.Development.json`; seed passwords are documented dev credentials.
- **Frontend**: zoneless Angular with signals; one `BankingApiService`; `localStorage` session; CORS (no dev proxy).

### Files created
Everything in the repository (initial commit series). Main entry points:
`backend/Banking.API/Program.cs`, `backend/Banking.Application/Services/TransactionService.cs`,
`backend/Banking.Domain/Entities/BankAccount.cs`, `backend/Banking.Infrastructure/Data/BankingDbContext.cs`,
`backend/Banking.Tests/Services/TransactionServiceTests.cs`, `frontend/banking-client/src/app/app.routes.ts`.

### Completed tasks
- [x] Research · [x] Backend · [x] Tests · [x] Frontend · [x] Docs · [x] Local verification · [x] GitHub publish

### Pending tasks
None required for v1.0.0. Optional ideas are listed under *Known limitations* in `PROJECT_HANDOFF.md`.

### Tests executed (actual results)
| Command | Result |
|---------|--------|
| `dotnet build backend/Banking.sln` (after deleting all bin/obj) | Build succeeded, 0 warnings, 0 errors |
| `dotnet test backend/Banking.sln` | Passed 36, Failed 0, Skipped 0 |
| `dotnet ef migrations has-pending-model-changes` | No changes since last migration |
| `npm run build` (frontend) | Success, initial bundle 367 kB (95 kB transferred) |
| API smoke script against LocalDB | 24/24 checks passed (401/403, validation, overdraft, transfer, 404, CORS) |
| Browser run (Angular + API) | Login, dashboard, transfer (+ overdraft block), server error display, Employee/Admin UI differences, account opening — all OK |

### Git state
- Branch: `main` (tracking `origin/main`).
- Last code commit before this handoff update: `086b959 feat(frontend): add Angular client for banking operations`.
- The commit that adds this file (`docs: ...`) is the HEAD of `main`; working tree clean after push.

### Exact next steps for the next session
1. `git pull`, then `cd backend && dotnet tool restore && dotnet test` to confirm a green baseline.
2. Pick an item from *Known limitations* (suggested order: paging on list endpoints → user management for Admins →
   refresh tokens → frontend unit tests → GitHub Actions CI running `dotnet test` and `npm run build`).
3. Keep the same layering: DTO → service → thin controller → Angular API method → page.
4. Update `PROJECT_HANDOFF.md`, `CHANGELOG.md` and this file before ending the session.
