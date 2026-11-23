# Architecture

## Goals

1. Easy to read top-to-bottom and explain in an interview.
2. Financial rules live in **one** place and are covered by tests.
3. No patterns or packages that don't pay for themselves.

## Layers and dependencies

```
Banking.API  ──►  Banking.Application  ──►  Banking.Domain
     │                    ▲                      ▲
     └──►  Banking.Infrastructure ───────────────┘
```

| Project | Depends on | Contains | Does NOT contain |
|---------|------------|----------|------------------|
| **Domain** | nothing | Entities (`User`, `Customer`, `BankAccount`, `Transaction`), enums, `DomainException`, balance rules | EF Core, HTTP, DTOs |
| **Application** | Domain | Services (use cases), DTOs, repository / unit-of-work / security interfaces, `NotFoundException`, `ConflictException` | SQL, EF queries, controllers |
| **Infrastructure** | Application, Domain | `BankingDbContext`, migrations, repositories, `UnitOfWork`, `JwtTokenService`, `PasswordHasher`, `DbSeeder` | Business decisions |
| **API** | Application, Infrastructure | Controllers, `ExceptionHandlingMiddleware`, `Program.cs` (DI, JWT, CORS, Swagger) | Business logic, data access |

Why Application defines the interfaces (`IBankAccountRepository`, `IUnitOfWork`, …): the business logic can be read and
tested without knowing anything about SQL Server; Infrastructure plugs in the real implementation (dependency inversion).
API references Infrastructure **only** to register it in `Program.cs`.

## Key design decisions

### Rules inside the entity
`BankAccount.Balance` has a **private setter**. The only ways to change it are `Deposit()` and `Withdraw()`, which validate
the amount (> 0, ≤ 1,000,000, max 2 decimals) and refuse to go below zero. Services can't "forget" a rule because there is
no other path. The database adds a second safety net: `CHECK (Balance >= 0)`.

### Plain service classes, no interfaces
`AuthService`, `CustomerService`, `AccountService`, `TransactionService`, `DashboardService` are concrete classes registered
in DI. They have only one implementation, so an interface would add files without adding value. Tests create them directly
with real repositories over an in-memory SQLite database.

### Specific repositories, no generic repository
Each repository exposes only the queries the services need (`GetByIdAsync`, `EmailExistsAsync`, `GetTotalBalanceAsync`, …).
Repositories **stage** changes (`Add`); `IUnitOfWork` **saves** them. That keeps "one request = one save" explicit.

### Transfers and database transactions
```csharp
await _unitOfWork.ExecuteInTransactionAsync(() =>
{
    from.Withdraw(amount);   // throws if balance is too low
    to.Deposit(amount);
    Record(from, TransferOut, ...);
    Record(to, TransferIn, ...);
    return Task.CompletedTask;
});
```
`UnitOfWork.ExecuteInTransactionAsync` = `BeginTransaction` → run the operation → `SaveChanges` → `Commit`. Any exception
skips the commit and disposing the transaction rolls it back.

### Concurrency
`Balance` is configured with `IsConcurrencyToken()`. EF Core generates
`UPDATE BankAccounts SET Balance = @new WHERE Id = @id AND Balance = @old`. If another request changed the balance in
between, 0 rows are affected, EF throws `DbUpdateConcurrencyException`, the transaction is rolled back and the API
answers **409 Conflict**. Covered by `ConcurrentWithdrawals_SecondStaleUpdateIsRejected`.

### Error handling
Services throw meaningful exceptions; `ExceptionHandlingMiddleware` maps them to HTTP status codes and ProblemDetails:

| Exception | HTTP |
|-----------|------|
| `DomainException` | 400 |
| `NotFoundException` | 404 |
| `ConflictException`, `DbUpdateConcurrencyException` | 409 |
| anything else | 500 (logged, generic message to client) |

DTO validation (data annotations) is handled automatically by `[ApiController]` → 400 with field errors.

### Authentication & authorization
- Passwords: PBKDF2 hashes via ASP.NET Core Identity's `PasswordHasher<T>` (only the hasher, not full Identity).
- Login returns a JWT with `sub`, `name`, `role`; signed with HMAC-SHA256 (`Jwt:Key`, min 32 chars, not committed for non-dev).
- `MapInboundClaims = false` + `NameClaimType = "name"`, `RoleClaimType = "role"` so `User.Identity.Name` and
  `[Authorize(Roles = "Admin")]` read exactly the claims we wrote.
- Every controller has `[Authorize]`; `POST /api/accounts` and the whole `UsersController` additionally have
  `[Authorize(Roles = Roles.Admin)]`. `AuthController` is `[AllowAnonymous]`.
- **Sessions**: the JWT lives 15 minutes. Login also returns a random 64-byte refresh token (7 days); only its SHA-256
  hash is stored in `RefreshTokens`. `AuthService.RefreshAsync` rotates it (old token revoked, new one issued);
  a revoked token presented again triggers revocation of all the user's tokens (reuse detection).
  Logout, deactivation and password resets revoke tokens. A JWT cannot be revoked, hence the short lifetime.
- **Deactivated users** (`Users.IsActive = false`) cannot log in or refresh. Users are never deleted because their
  username is kept in `Transactions.PerformedBy`. An admin cannot deactivate their own account.
- The Angular app hides admin-only buttons, but **the API is the authority** (an Employee calling the endpoint gets 403).

### Paging
- List endpoints bind `[FromQuery] PageQuery paging` (`page` ≥ 1, `pageSize` 1-100, validated by data annotations)
  and return `PagedResponse<T>` (`items`, `page`, `pageSize`, `totalCount`, `totalPages`).
- Repositories return `(Items, TotalCount)`: one `COUNT` and one `Skip/Take` query with a stable `ORDER BY`.
- The parameter is named `paging` on purpose: a parameter called `page` makes MVC model binding treat `page` as a
  prefix and silently ignore `?page=&pageSize=`.

### Logging
Built-in `ILogger<T>`: successful money movements and sign-ins at Information, failed logins and rule violations at Warning,
unexpected exceptions at Error with the stack trace.

## Frontend

```
src/app/
├── core/       api.config.ts, models.ts, banking-api.service.ts, auth.service.ts, auth.guard.ts (authGuard, adminGuard),
│               auth.interceptor.ts, error-message.ts, testing.ts (test-only routes), *.spec.ts
├── layout/     shell.component.ts (top navigation + <router-outlet>)
├── shared/     transaction-table.component.ts, pager.component.ts
└── pages/      login, dashboard, customers, accounts, account-detail, transfer, transactions, users (admin)
```

- **Session refresh**: `authInterceptor` catches a 401 on any non-`/auth/` request, calls `AuthService.refresh()`
  once (parallel 401s share the same in-flight call), stores the new pair and repeats the request. If the refresh
  fails, the session is cleared and the user lands on `/login`. `logout()` revokes the refresh token on the server.
- **Tests**: `ng test` (Angular's `@angular/build:unit-test` builder) runs Vitest in jsdom. `src/test-setup.ts`
  re-exposes jsdom's `localStorage`, which Node 25+ hides behind its own (undefined) global.

- **Standalone components + signals**: Angular 22 apps are zoneless; component state lives in `signal()`s so the view
  updates when HTTP responses arrive.
- **One API service** (`BankingApiService`) with one method per endpoint; components never build URLs.
- **`authInterceptor`** attaches the JWT and logs out on 401. **`authGuard`** protects every route except `/login`.
- **Reactive forms** mirror the server validators for instant feedback; the server still validates everything.
- Route params are bound to component inputs (`withComponentInputBinding`), e.g. `AccountDetailComponent.id`.
