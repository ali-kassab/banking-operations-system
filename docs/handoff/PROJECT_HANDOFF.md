# Project handoff

Read this first when continuing work in a new session. Last updated: **2026-11-22**.

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
| Node used during development | 26.13.1 (Angular 22 supports ^22.22.3, ^24.15.0, >=26) |

## 3. Architecture & responsibilities

See `docs/architecture.md` for the full explanation. Summary:

- `Banking.Domain` — entities + rules (`BankAccount.Deposit/Withdraw/ValidateAmount`), no dependencies.
- `Banking.Application` — services (concrete classes), DTOs, interfaces (`IRepositories.cs`, `IUnitOfWork`, `IPasswordHasher`, `ITokenService`), `DtoMapper`.
- `Banking.Infrastructure` — `BankingDbContext` (fluent config, check constraints, UTC converter), migrations, repositories, `UnitOfWork`, JWT, hashing, `DbSeeder`.
- `Banking.API` — thin controllers, `ExceptionHandlingMiddleware`, `Program.cs`.
- `Banking.Tests` — xUnit; `TestDatabase` helper gives each test a fresh in-memory SQLite DB.
- `frontend/banking-client` — Angular standalone app (signals, reactive forms, functional guard + interceptor).

## 4. Folder structure

```
backend/   Banking.sln, dotnet-tools.json, Banking.API/, Banking.Application/, Banking.Domain/, Banking.Infrastructure/, Banking.Tests/
frontend/  banking-client/ (src/app/core, layout, shared, pages/*)
docs/      architecture.md, project-overview.md, handoff/
```
Full tree: README → *Project structure*.

## 5. Implemented features

- [x] JWT login (`POST /api/auth/login`), 60-minute tokens
- [x] Roles Admin / Employee; Admin-only account opening
- [x] Customers: list, search, get, create (unique email + national ID)
- [x] Accounts: list (filter by customer), get, open with optional initial deposit
- [x] Deposit, withdraw
- [x] Transfer (explicit DB transaction, shared reference, two history rows)
- [x] Per-account history + global recent transactions
- [x] Dashboard summary
- [x] Centralized exception handling (ProblemDetails), logging, Swagger with JWT button
- [x] Dev seed data (2 users, 3 customers with funded accounts)
- [x] Angular pages: login, dashboard, customers, accounts, account detail (deposit/withdraw + history), transfer, transactions

## 6. Database schema

- `Users(Id, Username UQ, FullName, PasswordHash, Role[string], CreatedAt)`
- `Customers(Id, FirstName, LastName, Email UQ, PhoneNumber, NationalId UQ, CreatedAt)`
- `BankAccounts(Id, AccountNumber UQ, Type[string], Balance decimal(18,2) CHECK >= 0 [concurrency token], CustomerId FK→Customers RESTRICT, CreatedAt)`
- `Transactions(Id, BankAccountId FK→BankAccounts RESTRICT, Type[string], Amount decimal(18,2) CHECK > 0, BalanceAfter, Description, ReferenceNumber IX, CounterpartyAccountId NULL, PerformedBy, CreatedAt IX)`

Migration: `backend/Banking.Infrastructure/Data/Migrations/*_InitialCreate.cs`. Applied automatically at startup in Development.

## 7. API endpoints

`POST /api/auth/login` · `GET /api/dashboard/summary` · `GET|POST /api/customers` · `GET /api/customers/{id}` ·
`GET /api/accounts?customerId=` · `POST /api/accounts` (Admin) · `GET /api/accounts/{id}` ·
`POST /api/accounts/{id}/deposit` · `POST /api/accounts/{id}/withdraw` · `GET /api/accounts/{id}/transactions` ·
`POST /api/transfers` · `GET /api/transactions?take=`. Details in README → *API endpoints*.

## 8. Authentication & authorization design

- Hashing: `Infrastructure/Security/PasswordHasher.cs` wraps Identity's `PasswordHasher<User>` (PBKDF2).
- Token: `JwtTokenService` writes claims `sub`, `name`, `role`, `jti`; HMAC-SHA256 with `Jwt:Key`.
- Validation (`Program.cs`): issuer, audience, signing key, lifetime (1 min skew); `MapInboundClaims=false`;
  `NameClaimType="name"`, `RoleClaimType="role"`.
- `Jwt:Key` is empty in `appsettings.json`; a dev-only key is in `appsettings.Development.json`. API fails fast if key < 32 chars.
- Frontend stores the login response in `localStorage` (`banking.session`), drops it when expired, logs out on 401.

## 9. Business rules (where enforced)

| Rule | Where |
|------|-------|
| Amount > 0, ≤ 1,000,000, ≤ 2 decimals | `BankAccount.ValidateAmount` (+ DTO `[Range]`, + Angular validators) |
| No negative balance / no overdraft | `BankAccount.Withdraw` + DB `CHECK (Balance >= 0)` |
| Transfer source ≠ destination | `TransactionService.TransferAsync` |
| Transfer atomicity | `UnitOfWork.ExecuteInTransactionAsync` |
| Lost-update protection | `Balance.IsConcurrencyToken()` → 409 |
| Unique email / national ID | `CustomerService.CreateAsync` + unique indexes |
| Admin-only account opening | `[Authorize(Roles = Roles.Admin)]` on `AccountsController.Create` |

## 10. Current status

**Complete and working.** Backend and frontend build without warnings, all 36 backend tests pass, the API was smoke-tested
against SQL Server LocalDB, and the Angular UI was exercised end-to-end in a browser (login, dashboard, transfers,
overdraft errors, role-based UI, account opening).

## 11. Known limitations / outstanding issues

- No refresh tokens, logout is client-side only (token remains valid until expiry).
- No user management endpoints/UI; users exist only via the seeder.
- No paging on list endpoints.
- No frontend automated tests (template generated with `--skip-tests`).
- JWT stored in `localStorage` (simple; an HttpOnly cookie would be more XSS-resistant).
- No CI pipeline or Docker setup.

## 12. Build & test status (2026-11-22)

| Check | Command | Result |
|-------|---------|--------|
| Backend build | `dotnet build backend/Banking.sln` | ✅ 0 warnings, 0 errors |
| Backend tests | `dotnet test backend/Banking.sln` | ✅ 36 passed, 0 failed |
| Frontend build | `npm run build` (in `frontend/banking-client`) | ✅ success (~367 kB initial, ~95 kB transferred) |
| API smoke test vs LocalDB | manual PowerShell script | ✅ 24/24 checks (auth, 401/403, rules, transfer, CORS) |
| UI end-to-end | manual in browser | ✅ |

## 13. How to continue development

1. `cd backend && dotnet tool restore && dotnet build && dotnet test`
2. `dotnet run --project Banking.API` (DB migrated + seeded automatically in Development).
3. `cd frontend/banking-client && npm install && npm start` → http://localhost:4200.
4. Schema change: edit entity + `BankingDbContext`, then
   `dotnet ef migrations add <Name> --project Banking.Infrastructure --startup-project Banking.API --output-dir Data/Migrations`.
5. New endpoint: DTO in `Application/Dtos` → method in a service → thin controller action → API method in
   `banking-api.service.ts` → page component. Add tests in `Banking.Tests`.
6. Update `SESSION_HANDOFF.md` and `CHANGELOG.md` at the end of every session.
