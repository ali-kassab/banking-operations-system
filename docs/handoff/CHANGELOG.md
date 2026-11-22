# Changelog

All notable changes, newest first.

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
