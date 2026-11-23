# Session handoff

## Session: 2026-11-23 (3) — v1.1: paging, user management, refresh tokens, frontend tests, CI

### Overview
Portfolio banking back-office (ASP.NET Core 10 + EF Core/SQL Server + Angular 22). The user asked to "complete the
project": all five *Known limitations* items from v1.0 were implemented, in the suggested order. The user also decided
**no Docker** in this project (now recorded in `PROJECT_HANDOFF.md` §10).

### Completed (all on branch `feature/v1.1-known-limitations`, created from `fix/clear-form-errors-on-cancel`)
| Commit | What |
|--------|------|
| `6582753` | API: paging (`PageQuery`/`PagedResponse<T>`), `UsersController` + `UserService`, refresh tokens (`RefreshToken` entity, rotation, reuse detection, logout), migration `AddUserManagementAndRefreshTokens`, 30 new tests |
| `f8c9930` | Fix: `?page=&pageSize=` were ignored because the action parameter was named `page` (renamed to `paging`) — found by the smoke test |
| `4d47aa2` | Frontend: pager component, paged pages, Users page + `adminGuard`, interceptor refresh-and-retry, logout revocation, topbar fit |
| `5923547` | Frontend tests: Vitest 4.1.12 + jsdom 31.0.0 via `@angular/build:unit-test`, 31 specs, `src/test-setup.ts` |
| `7369b68` | `.github/workflows/ci.yml` (backend build `-warnaserror` + tests, frontend `npm ci` + build + tests) |
| (docs commit) | README, `docs/architecture.md`, `docs/project-overview.md`, PROJECT_HANDOFF, CHANGELOG 1.1.0, this file |

### Verification (actual results)
| Check | Result |
|-------|--------|
| `dotnet build Banking.sln -c Release -warnaserror` | 0 warnings, 0 errors |
| `dotnet test` | 66 passed, 0 failed |
| `dotnet ef migrations has-pending-model-changes` | No changes |
| `npm run build` | Success, 381.68 kB initial (98.42 kB transferred), no warnings |
| `npm test` | 6 files, 31 passed |
| `npm ci --dry-run` (npm 10.8.2) | OK — lockfile accepted |
| API smoke script vs LocalDB (new API on port 5081) | 35/35 (first run 33/35 → found and fixed the paging-binding bug) |
| Browser (Angular dev server on 4200 → API 5081) | Login; customers/transactions paging (Next/Prev, disabled states); 401 → single refresh → retry; Users page create (client-side password rule), deactivate; sign-out calls `/auth/logout` (204); employee sees no Users link and `/users` redirects; no horizontal overflow at 375px |
| GitHub Actions run 45088681548 (commit `b5a8d63`) | ✅ Backend 40 s, Frontend 24 s. Only warning: v4 actions target deprecated Node 20 → bumped to v5 (Node 24) in a follow-up commit |

### Environment notes / side effects (local only)
- The LocalDB database `BankingOpsDb` was **migrated** to `AddUserManagementAndRefreshTokens`.
- Test data added there: deactivated users `smoke17910`, `smoke25132` (smoke script) and `ui.tester` (browser test);
  a 5.00 deposit on account `5504380647` (customer "Test Smoke"). Nothing deleted.
- An **old API process from the previous session (PID 21840, `Banking.API.exe`, port 5080) and the old Angular dev
  server (4200) were still running**; I was not permitted to stop them. The 4200 server now serves the new frontend,
  but the 5080 API is still the **old** build, so the running app is inconsistent until the API is restarted:
  stop it, then `cd backend; dotnet run --project Banking.API`.
- The browser test temporarily pointed `API_URL` to 5081; this was reverted (`api.config.ts` unchanged in git).
- npm 10.8.2 crashes with `Cannot read properties of null (reading 'edgesOut')` when installing Vitest; it was installed
  with `npx npm@11.21.2 install --save-dev --save-exact vitest@4.1.12`.

### Git / publishing state
- Branch `feature/v1.1-known-limitations` pushed to `origin` with the user's approval (remote head `b5a8d63` verified).
  The follow-up commit bumping the CI actions to v5 + this handoff update is local until the user approves another push.
  Not merged; no PR. `main` unchanged at `9f02286`.
- Earlier branches `docs/session-2026-11-23` and `fix/clear-form-errors-on-cancel` are pushed; this branch contains both.

### Exact next step for the next session
1. If the CI-actions/handoff commit is still local, ask for approval to push it (`git push`), then check that the
   GitHub Actions run passes without the Node 20 warning.
2. Restart the local API (see above) before demoing.
3. Ask the user whether to open a PR into `main` (never merge without explicit approval).
4. No further roadmap is approved. Possible future ideas (only if the user asks): HttpOnly-cookie refresh token,
   searchable pickers instead of the 100-item dropdowns, browser end-to-end tests.

### Constraints to keep
- Readability over features; no MediatR/CQRS/generic repositories/AutoMapper/UI libraries; **no Docker**.
- Layering: DTO → service → thin controller → Angular API method → page; paging parameter named `paging`.
- Git identity `ali-kassab`; no AI attribution in commits; push/merge/PR only with explicit approval; never touch `main`.

---

## Session: 2026-11-23 (2) — fix stale form errors on Cancel

### Project / release status
- v1.0.0 is complete and published (`main` = `9f02286`). There is **no further approved roadmap** (no R-numbered releases);
  the *Known limitations* list in `PROJECT_HANDOFF.md` is optional ideas, to be started only when the user picks one.

### Task completed
- Fixed the cosmetic issue found in the demo: after a failed create (e.g. duplicate email), pressing **Cancel** left the red
  error message visible. The Cancel / "+ New" toggle button now also runs `error.set('')`.
- Files: `frontend/banking-client/src/app/pages/customers/customers.component.html`,
  `frontend/banking-client/src/app/pages/accounts/accounts.component.html` (one line each). No other code changed.
- Reused the existing `error` signal; no new dependencies.

### Checks run (actual results)
| Command | Result |
|---------|--------|
| `npm run build` (frontend/banking-client) | Success, initial bundle 367.18 kB (95.46 kB transferred) |
| Browser check of the fix | **Not done** (needs API + LocalDB running) |
| Backend tests | Not re-run (no backend change); last run 36/36 passed |

### Git / publishing state
- Branch **`fix/clear-form-errors-on-cancel`**, created from `docs/session-2026-11-23` (so it also contains that branch's
  handoff commits), pushed to `origin` with the user's approval. Not merged; no PR opened.
- `main` unchanged at `9f02286`.

### Exact next step for the next session
1. Ask the user whether to open PRs for `docs/session-2026-11-23` and/or `fix/clear-form-errors-on-cancel` into `main`
   (never merge or push to `main` without explicit approval).
2. Optionally verify the fix in the browser: start the API + `npx ng serve`, sign in as `admin`, create a customer with a
   duplicate email, press Cancel → the error message must disappear (same check on the Accounts page).
3. Further work only if the user picks an item from *Known limitations* (suggested order: paging → user management →
   refresh tokens → frontend tests → CI).

### Constraints to keep
- Readability over features; no MediatR/CQRS/generic repositories/AutoMapper/UI libraries.
- Layering: DTO → service → thin controller → Angular API method → page.
- Git identity `ali-kassab`; no AI attribution in commits; push/merge/PR only with explicit approval; never touch `main`.

---

## Session: 2026-11-23 — live demo (no code changes)

### Project / release status
- **v1.0.0 is complete and published** at https://github.com/ali-kassab/banking-operations-system (`main` = `9f02286`).
- This session changed **no source code**. The only file edited is this handoff file.

### What was done
- Started the API (`dotnet run --project Banking.API`, http://localhost:5080) and the Angular dev server
  (`npx ng serve`, http://localhost:4200) using the existing Development configuration (LocalDB `BankingOpsDb`).
- Walked through the app in the browser pane as **Admin**:
  - login → dashboard (live totals and recent transactions)
  - Customers: created the fictional customer **Nadia Demo** (`nadia.demo@example.com`, `NID-DEMO-01`)
  - showed the duplicate-email rule (409 message shown in the UI)
  - showed server-side search ("khalil")
  - Accounts list as Admin: the "+ Open account" button is visible
- The servers were restarted once because the earlier session's background processes had ended. The log showed no crash.

### Demo steps NOT completed (stopped at the user's request for a safe shutdown)
- Open an account for Nadia Demo; deposit/withdraw on it; transfer; show updated balances; transaction history page.
- Sign out → sign in as `employee` → show the permission differences (no "Open account" button; `POST /api/accounts` → 403).
  These flows were already verified in the 2026-11-22 session (see below), but were not re-shown in this demo.

### Data changes (local dev database only)
- LocalDB `BankingOpsDb` now also contains the customer "Nadia Demo" (0 accounts). Nothing was deleted.
- Earlier smoke-test data (customer "Test Smoke", account 7528224661 for Omar Khalil, a few transfers) also remains.

### Checks run this session (actual results)
| Command | Result |
|---------|--------|
| `dotnet test backend/Banking.Tests` | Passed 36, Failed 0, Skipped 0 |
| `npm run build` (frontend/banking-client) | Success, initial bundle 367.15 kB (95.44 kB transferred) |
| `git status` before the handoff edit | clean; `main` in sync with `origin/main` |

### Known issues found
- **Cosmetic (Customers page):** after a failed "Create customer" (e.g. duplicate email), pressing **Cancel** hides the form
  but the red error message stays visible until the next successful action. Fix: clear `error` in the Cancel/toggle
  handler in `frontend/banking-client/src/app/pages/customers/customers.component.html` (the same pattern exists on the Accounts page).
  Not fixed: the user asked for no design or code changes during the demo.

### Git / publishing state
- `main` is unchanged at `9f02286` (in sync with `origin/main`).
- With the user's approval, this handoff update was committed on the new branch **`docs/session-2026-11-23`**
  (created from `main`) and pushed to `origin/docs/session-2026-11-23`. It was **not** merged into `main`.

### Exact next step for the next session
1. Ask the user whether to open a pull request from `docs/session-2026-11-23` into `main`, or merge it themselves.
   Never merge or push to `main` without explicit approval.
2. If the user wants the demo finished: start both servers (commands above), sign in as `admin`, open an account for
   Nadia Demo, then deposit → withdraw → transfer → Transactions page → sign out → sign in as `employee` and show the
   missing "Open account" button.
3. Optional small fix: clear stale error messages on Cancel (Customers and Accounts pages).

---

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
