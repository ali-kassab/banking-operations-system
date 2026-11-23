# Changelog

All notable changes, newest first.

## [1.1.0] — 2026-11-23 (branch `feature/v1.1-known-limitations`, not merged)

### Added
- **Paging** on all list endpoints (`?page=&pageSize=`, max 100) returning `PagedResponse<T>`; shared Angular pager.
  `GET /api/transactions?take=` was replaced by `?page=&pageSize=`.
- **User management** (Admin): `GET|POST /api/users`, `PUT /api/users/{id}/status`, `PUT /api/users/{id}/password`;
  `Users.IsActive`; Users page and `adminGuard` in the client.
- **Refresh tokens**: 15-minute JWTs plus single-use 7-day refresh tokens (hashed, rotated, reuse detection);
  `POST /api/auth/refresh`, `POST /api/auth/logout`; the client refreshes once on 401 and retries.
- Migration `AddUserManagementAndRefreshTokens`.
- 30 backend tests (66 total) and 31 frontend unit tests (Vitest via `ng test`).
- GitHub Actions CI building and testing backend and frontend.

### Changed
- `LoginResponse` now includes `refreshToken` and `refreshTokenExpiresAt`; usernames are stored and matched lower-case.
- `Jwt:ExpiryMinutes` 60 → 15; new `Jwt:RefreshTokenDays` (7).
- Topbar fits all links on one line at 1024px (full name shown from 1100px, otherwise as the role badge tooltip).

### Fixed
- Customers and Accounts pages: pressing Cancel after a failed create now clears the error message.
- Paging query parameters were ignored when the action parameter was named `page` (found by the smoke test before release).

## [1.0.0] — 2026-11-22

### Added
- Repository layout: `backend/` (.NET solution), `frontend/` (Angular), `docs/`.
- **Domain**: `User`, `Customer`, `BankAccount`, `Transaction`; enums; `DomainException`.
  Balance changes only through `Deposit`/`Withdraw` with amount validation and overdraft protection.
- **Application**: `AuthService`, `CustomerService`, `AccountService`, `TransactionService`, `DashboardService`;
  DTOs with data-annotation validation; entity-specific repository interfaces; `IUnitOfWork` with
  `ExecuteInTransactionAsync` for transfers.
- **Infrastructure**: `BankingDbContext` (unique indexes, check constraints, `Balance` concurrency token,
  UTC date converter), `InitialCreate` migration, repositories, `UnitOfWork`, JWT token service,
  PBKDF2 password hasher, development seeder.
- **API**: controllers for auth, dashboard, customers, accounts, transfers/transactions; JWT bearer auth;
  `Admin`-only account opening; ProblemDetails exception middleware; CORS for `http://localhost:4200`; Swagger UI.
- **Tests**: 36 xUnit tests (domain rules, deposits/withdrawals, transfers incl. rollback and concurrency,
  account opening, customer uniqueness, login/JWT, password hashing) on in-memory SQLite.
- **Frontend**: Angular 22 app with login, dashboard, customers, accounts, account detail
  (deposit/withdraw + history), transfer and transactions pages; auth guard and interceptor.
- Documentation: README, architecture, project overview, handoff files; MIT license.

### Decisions
- .NET 10 LTS chosen over .NET 11 (STS) for long-term support; EF Core packages pinned to 10.0.x.
- Concrete service classes (no service interfaces) and no generic repository, to keep the code easy to follow.
- SQLite in-memory (not EF InMemory) for tests so transactions and constraints behave like a real database.
- Dates are stored in UTC and marked as UTC when read so the client shows correct local times.
