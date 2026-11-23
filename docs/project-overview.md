# Project overview

## What it is

A back-office tool for bank staff. It is **not** a customer-facing online-banking app: the people who sign in are
employees (`Admin` or `Employee`), and they act on behalf of customers.

## Users and permissions

| Action | Employee | Admin |
|--------|:--------:|:-----:|
| Sign in, view dashboard | ✅ | ✅ |
| View / search / create customers | ✅ | ✅ |
| View accounts and their history | ✅ | ✅ |
| Deposit, withdraw, transfer | ✅ | ✅ |
| **Open a new bank account** | ❌ (403) | ✅ |
| **Manage users** (create, activate/deactivate, reset password) | ❌ (403) | ✅ |

## Typical workflow

1. Employee signs in → dashboard shows totals and the latest activity.
2. A new customer arrives → **Customers → New customer**.
3. An Admin opens an account for them → **Accounts → Open account** (optionally with an initial deposit).
4. Cash operations → open the account → **Deposit / withdraw**.
5. Moving money → **Transfer** (source balance shown; overdrafts are blocked in the UI and by the API).
6. Review → account page history or **Transactions** page.

## Domain vocabulary

- **Customer** — a person who owns bank accounts.
- **Bank account** — has a unique 10-digit number, a type (Checking/Savings) and a balance (never negative).
- **Transaction** — one history entry: `Deposit`, `Withdrawal`, `TransferIn` or `TransferOut`, with the
  balance right after it (`BalanceAfter`) and who performed it.
- **Reference number** — `TXN-yyyyMMdd-XXXXXXXX`; both sides of a transfer share the same one.

## Scope decisions

Added in v1.1: user management, refresh tokens, paging, frontend unit tests and CI.
Deliberately left out to keep the project small and readable: multi-currency, interest/fees, account closing,
audit tables. **Docker is intentionally not used** (owner decision). See *Limitations* in the README and
*Outstanding issues* in `handoff/PROJECT_HANDOFF.md`.

## Reference projects reviewed

Before implementation, a few public GitHub projects were reviewed for structure only (no code was copied):

- `evgenirusev/BankManagementSystem` (MIT) — layered Data/Services/Web split; uses a generic `IRepository<T>`,
  which this project intentionally avoids in favour of small entity-specific repositories.
- `anton-morhunov/banking-api-dotnet` (MIT) — ASP.NET Core banking API with JWT and separate client applications;
  confirmed the idea of keeping the client in its own folder with its own dependencies.
