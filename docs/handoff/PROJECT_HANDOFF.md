# Project handoff

Read this first when continuing work in a new session. Last updated: **2026-11-23** (v1.1).

## 1. Purpose

Portfolio / interview project: a small **Banking Operations Management System** (bank back-office) showing
ASP.NET Core Web API, EF Core + SQL Server, layered architecture, JWT + role-based authorization and an Angular client.
Priority is **readability over features**. Do not add MediatR, CQRS, generic repositories, AutoMapper or UI libraries.

## 2. Stack & versions

| Component | Version |
|-----------|---------|
| .NET SDK / target | 10.0 (LTS), `net10.0` |
| EF Core (SqlServer, Design, Sqlite for tests) | 10.0.15 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.15 |
| Microsoft.Extensions.Identity.Core (password hasher only) | 10.0.15 |
| System.IdentityModel.Tokens.Jwt | 8.23.0 |
| Microsoft.Extensions.Logging.Abstractions (Application) | 10.0.15 |
| Swashbuckle.AspNetCore | 10.3.1 |
| xUnit / runner / Test SDK | 2.9.3 / 3.1.4 / 17.14.1 |
| dotnet-ef (local tool, `backend/dotnet-tools.json`) | 10.0.15 |
| Angular (core, cli, build) | 22.3.x |
| TypeScript | ~6.0 |
| Vitest / jsdom (frontend unit tests, devDependencies, pinned exact) | 4.1.12 / 31.0.0 |
| Node used during development | 26.13.1 (Angular 22 supports ^22.22.3, ^24.15.0, >=26); CI uses Node 24 |
| CI | GitHub Actions (`actions/checkout@v4`, `setup-dotnet@v4` 10.0.x, `setup-node@v4` 24) |

## 3. Architecture & responsibilities

See `docs/architecture.md` for the full explanation. Summary:

- `Banking.Domain` — entities + rules (`BankAccount.Deposit/Withdraw/ValidateAmount`, `RefreshToken.IsActive/Revoke`), no dependencies.
- `Banking.Application` — services (concrete classes), DTOs, interfaces (`IRepositories.cs`, `IUnitOfWork`, `IPasswordHasher`, `ITokenService`), `DtoMapper`, `PageQuery`/`PagedResponse<T>`.
- `Banking.Infrastructure` — `BankingDbContext` (fluent config, check constraints, UTC converter), migrations, repositories, `UnitOfWork`, JWT + refresh tokens, hashing, `DbSeeder`.
- `Banking.API` — thin controllers, `ExceptionHandlingMiddleware`, `Program.cs`.
- `Banking.Tests` — xUnit; `TestDatabase` helper gives each test a fresh in-memory SQLite DB and builds services.
- `frontend/banking-client` — Angular standalone app (signals, reactive forms, functional guards + interceptor), Vitest unit tests.

## 4. Folder structure

```
.github/workflows/ci.yml
backend/   Banking.sln, dotnet-tools.json, Banking.API/, Banking.Application/, Banking.Domain/, Banking.Infrastructure/, Banking.Tests/
frontend/  banking-client/ (src/app/core, layout, shared, pages/*; src/test-setup.ts; tsconfig.spec.json)
docs/      architecture.md, project-overview.md, handoff/
```
Full tree: README → *Project structure*.

## 5. Implemented features

- [x] JWT login (`POST /api/auth/login`), 15-minute access tokens + 7-day single-use refresh tokens (v1.1)
- [x] Refresh with rotation and reuse detection (`POST /api/auth/refresh`), server-side logout (`POST /api/auth/logout`) (v1.1)
- [x] Roles Admin / Employee; Admin-only account opening and user management
- [x] Users (Admin): list, create, activate/deactivate (not self), reset password (v1.1)
- [x] Customers: list (paged), search, get, create (unique email + national ID)
- [x] Accounts: list (paged, filter by customer), get, open with optional initial deposit
- [x] Deposit, withdraw
- [x] Transfer (explicit DB transaction, shared reference, two history rows)
- [x] Per-account history + global transactions (both paged)
- [x] Dashboard summary
- [x] Centralized exception handling (ProblemDetails), logging, Swagger with JWT button
- [x] Dev seed data (2 users, 3 customers with funded accounts)
- [x] Angular pages: login, dashboard, customers, accounts, account detail, transfer, transactions, users (admin); shared pager
- [x] Frontend unit tests (Vitest) and GitHub Actions CI (v1.1)

## 6. Database schema

- `Users(Id, Username UQ [stored lower-case], FullName, PasswordHash, Role[string], IsActive, CreatedAt)`
- `RefreshTokens(Id, UserId FK→Users CASCADE, TokenHash nvarchar(64) UQ [SHA-256 hex], ExpiresAt, CreatedAt, RevokedAt NULL)`
- `Customers(Id, FirstName, LastName, Email UQ, PhoneNumber, NationalId UQ, CreatedAt)`
- `BankAccounts(Id, AccountNumber UQ, Type[string], Balance decimal(18,2) CHECK >= 0 [concurrency token], CustomerId FK→Customers RESTRICT, CreatedAt)`
- `Transactions(Id, BankAccountId FK→BankAccounts RESTRICT, Type[string], Amount decimal(18,2) CHECK > 0, BalanceAfter, Description, ReferenceNumber IX, CounterpartyAccountId NULL, PerformedBy, CreatedAt IX)`

Migrations (`backend/Banking.Infrastructure/Data/Migrations/`): `*_InitialCreate`, `20261122235938_AddUserManagementAndRefreshTokens`
(hand-edited: `IsActive` default **true** for existing rows; the model itself has no DB default on purpose, because EF
would ignore an explicit `false` on insert). Applied automatically at startup in Development.

## 7. API endpoints

`POST /api/auth/login|refresh|logout` · `GET /api/dashboard/summary` · `GET|POST /api/customers` · `GET /api/customers/{id}` ·
`GET /api/accounts?customerId=` · `POST /api/accounts` (Admin) · `GET /api/accounts/{id}` ·
`POST /api/accounts/{id}/deposit` · `POST /api/accounts/{id}/withdraw` · `GET /api/accounts/{id}/transactions` ·
`POST /api/transfers` · `GET /api/transactions` · `GET|POST /api/users`, `PUT /api/users/{id}/status|password` (Admin).
All list endpoints take `page`/`pageSize` (default 1/20, max 100) and return `PagedResponse<T>`. Details in README → *API endpoints*.

## 8. Authentication & authorization design

- Hashing: `Infrastructure/Security/PasswordHasher.cs` wraps Identity's `PasswordHasher<User>` (PBKDF2).
- Access token: `JwtTokenService` writes claims `sub`, `name`, `role`, `jti`; HMAC-SHA256 with `Jwt:Key`; `Jwt:ExpiryMinutes` = 15.
- Refresh token: 64 random bytes (Base64Url), `Jwt:RefreshTokenDays` = 7, only the SHA-256 hash is stored. Single-use:
  `AuthService.RefreshAsync` revokes the used token and issues a new pair; a revoked token presented again revokes
  every session of that user. Logout, deactivation and password reset revoke tokens.
- Validation (`Program.cs`): issuer, audience, signing key, lifetime (1 min skew); `MapInboundClaims=false`;
  `NameClaimType="name"`, `RoleClaimType="role"`.
- `Jwt:Key` is empty in `appsettings.json`; a dev-only key is in `appsettings.Development.json`. API fails fast if key < 32 chars.
- Login usernames are trimmed and lower-cased; login fails identically for unknown user, wrong password and deactivated user.
- Password policy (create/reset): 8-100 chars, at least one letter and one digit (`PasswordRules` + Angular `PASSWORD_PATTERN`).
- Frontend stores the login response in `localStorage` (`banking.session`); keeps it while the refresh token is valid;
  the interceptor refreshes once on 401 and retries; logout calls `/auth/logout`.

## 9. Business rules (where enforced)

| Rule | Where |
|------|-------|
| Amount > 0, ≤ 1,000,000, ≤ 2 decimals | `BankAccount.ValidateAmount` (+ DTO `[Range]`, + Angular validators) |
| No negative balance / no overdraft | `BankAccount.Withdraw` + DB `CHECK (Balance >= 0)` |
| Transfer source ≠ destination | `TransactionService.TransferAsync` |
| Transfer atomicity | `UnitOfWork.ExecuteInTransactionAsync` |
| Lost-update protection | `Balance.IsConcurrencyToken()` → 409 |
| Unique email / national ID | `CustomerService.CreateAsync` + unique indexes |
| Unique username | `UserService.CreateAsync` + unique index |
| Admin-only account opening / user management | `[Authorize(Roles = Roles.Admin)]` on `AccountsController.Create` and `UsersController` |
| Admin cannot deactivate self | `UserService.SetActiveAsync` (400) |
| Deactivated users cannot sign in / refresh | `AuthService.LoginAsync` / `RefreshAsync` |

## 10. Permanent decisions & conventions

- **No Docker** in this project (owner decision, 2026-11-23). Do not add Dockerfiles or docker-compose.
- Layering for new endpoints: DTO → service → thin controller → Angular API method → page; tests in `Banking.Tests`
  and `*.spec.ts` next to the code.
- Paging: controller parameter must be named `paging` (`[FromQuery] PageQuery paging`), never `page` — see
  `docs/architecture.md` → *Paging*.
- Users are never deleted, only deactivated (usernames remain in `Transactions.PerformedBy`).
- Frontend dependencies are pinned to versions at least two weeks old. npm 10.8.2 crashes (`edgesOut`) when adding
  Vitest; `npx npm@11.21.2 install ...` was used for that one install. `npm ci` with npm 10 accepts the lockfile.
- Commits use the user's identity (`ali-kassab`); no AI attribution. Never push to or merge into `main` without
  explicit approval; work on feature branches.

## 11. Current status

**v1.1 complete on branch `feature/v1.1-known-limitations`** (not merged to `main`; `main` is still v1.0.0).
Backend builds with 0 warnings (`-warnaserror`), 66 backend tests and 31 frontend tests pass, the API was smoke-tested
against LocalDB (35/35 checks), and the Angular UI was exercised in a browser (paging, refresh-on-401, users page,
sign-out revocation, employee restrictions, 375px layout).

## 12. Known limitations / outstanding issues

- JWT and refresh token stored in `localStorage` (simple; an HttpOnly cookie would be more XSS-resistant).
- An issued access token stays valid until expiry (≤ 15 min) after logout/deactivation.
- Customer/account pickers (open account, transfer) load only the first 100 entries.
- No browser end-to-end tests (frontend tests are unit tests).
- GitHub Actions workflow verified locally only until it runs on GitHub.

## 13. Build & test status (2026-11-23)

| Check | Command | Result |
|-------|---------|--------|
| Backend build | `dotnet build backend/Banking.sln -c Release -warnaserror` | ✅ 0 warnings, 0 errors |
| Backend tests | `dotnet test backend/Banking.sln` | ✅ 66 passed, 0 failed |
| Migrations in sync | `dotnet ef migrations has-pending-model-changes` | ✅ no changes |
| Frontend build | `npm run build` (in `frontend/banking-client`) | ✅ success (~382 kB initial, ~98 kB transferred) |
| Frontend tests | `npm test` | ✅ 31 passed |
| API smoke test vs LocalDB | scripted curl checks | ✅ 35/35 (paging, users, refresh/reuse/logout, 401/403) |
| UI in browser | manual | ✅ |

## 14. How to continue development

1. `cd backend && dotnet tool restore && dotnet build && dotnet test`
2. `dotnet run --project Banking.API` (DB migrated + seeded automatically in Development).
3. `cd frontend/banking-client && npm install && npm start` → http://localhost:4200; `npm test` for unit tests.
4. Schema change: edit entity + `BankingDbContext`, then
   `dotnet ef migrations add <Name> --project Banking.Infrastructure --startup-project Banking.API --output-dir Data/Migrations`.
5. New endpoint: DTO in `Application/Dtos` → method in a service → thin controller action → API method in
   `banking-api.service.ts` → page component. Add tests in `Banking.Tests` and a `*.spec.ts`.
6. Update `SESSION_HANDOFF.md` and `CHANGELOG.md` at the end of every session.
