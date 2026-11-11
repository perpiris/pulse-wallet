# PulseWallet

A digital wallet API built with .NET 10 and domain-driven design. Users create a wallet with a unique username and a base currency, top it up, withdraw from it and send money to other wallets, with currency conversion when the wallets use different currencies.

PulseWallet is a learning project: the aim is a realistic fintech backend that shows clean architecture, DDD and production-minded practices, built step by step.

## Goals

**Features**
- Create a wallet with a unique username and a base currency
- Top up and withdraw
- Transfer money between wallets
- Convert currencies using live exchange rates when wallets use different currencies
- Send money to several users at once (for example `@user1 @user2`) using background jobs

**Engineering**
- Domain-driven design: business rules live in the domain model, not in controllers or services
- Clean architecture: dependencies point inward, and the domain has no dependencies
- Reliable money handling: decimals, consistent balances, transaction history
- Runs with one command in Docker

**Testing**
- Unit tests for the domain model (`Wallet` rules, money operations) and the use case handlers
- Integration tests for the API against a real SQL Server (Testcontainers)
- Architecture tests that fail the build if a layer depends on the wrong project

**Advanced backend techniques**
These are the problems real payment systems have to solve, and the main things this project is meant to explore:
- **Idempotency:** an `Idempotency-Key` header on money-moving requests, so a retried request never transfers money twice
- **Concurrency control:** optimistic concurrency on wallets, so two simultaneous withdrawals can't overdraw a balance
- **Resilience:** timeouts, retries with backoff and circuit breakers around external calls (the exchange rate API)
- **Caching:** exchange rates cached for a short time, never used once they're too old
- **Domain events and the transactional outbox:** "money received" style events that are never lost, even if the app crashes after saving
- **Background jobs:** batch transfers and other long-running work processed outside the HTTP request
- **Ledger:** every balance change recorded as an append-only transaction, so balances can be audited and rebuilt
- **Validation and ProblemDetails:** consistent, clear errors for every endpoint
- **Rate limiting:** protect the API from abuse
- **Observability:** structured logs plus OpenTelemetry tracing, so one request can be followed through every layer

## Frontend

A web client for creating wallets, viewing balances and transaction history, and sending money. The framework hasn't been decided yet:
- **React:** the most widely used, with a large ecosystem
- **Angular:** batteries included, common in enterprise and banking
- **Blazor:** C# end to end, sharing models with the API

## Optional / future

Secondary for this project, possible later:
- CI pipeline (build and tests on every push, GitHub Actions)
- Cloud deployment (Azure)
- Container orchestration

## Progress

**Foundation**
- [x] Solution structure (Api, Application, Domain, Infrastructure)
- [x] Central package management and shared build settings
- [x] Structured logging with Serilog
- [x] OpenAPI document and Swagger UI
- [x] Health check including SQL Server
- [x] Docker Compose with SQL Server

**Wallets**
- [x] `Wallet` entity with username rules
- [x] EF Core mapping for wallets
- [x] Create wallet: save to the database, return 201 with `Location`
- [x] Get wallet by ID (200 / 404)
- [x] Error handling with ProblemDetails (400 / 404 / 409 / 422 / 500)
- [x] Database migrations
- [ ] Apply migrations automatically at startup (Development only)

**Transactions**
- [x] `Money` value object (amount + currency, no negatives, at most 2 decimals)
- [x] `LedgerEntry` model and migration (`AddLedgerEntries`)
- [x] Wallet details include the 10 latest ledger entries
- [x] Top up and withdraw (`POST /api/wallets/{id}/top-ups`, `/withdrawals`; 422 on insufficient funds)
- [ ] Full transaction history (paged)
- [ ] Transfers between wallets
- [ ] Exchange rates client
- [ ] Transfers between wallets in different currencies

**Testing**
- [ ] Domain unit tests
- [ ] Handler unit tests
- [ ] API integration tests (Testcontainers)
- [ ] Architecture tests

**Advanced**
- [x] Optimistic concurrency (row version on wallets, 409 on conflict) + read committed snapshot isolation
- [ ] Idempotency keys
- [ ] Resilience for the exchange rate client (timeouts, retries, circuit breaker)
- [ ] Exchange rate caching
- [ ] Domain events + transactional outbox
- [ ] Batch transfers with background jobs
- [ ] Rate limiting
- [ ] OpenTelemetry tracing
- [ ] Authentication

**Frontend**
- [ ] Choose a framework (React / Angular / Blazor)
- [ ] Create wallet, balance and history screens
- [ ] Send money

**Optional / future**
- [ ] CI pipeline
- [ ] Cloud deployment

## Architecture

```
src/
  PulseWallet.Api              HTTP layer: controllers, requests/responses, error handling, composition root
  PulseWallet.Application      Use cases: one handler per operation, commands, results
  PulseWallet.Domain           Business model and rules: entities, value objects, domain exceptions, repository interfaces
  PulseWallet.Infrastructure   Technical details: EF Core, SQL Server, repository and query implementations
```

Dependencies point inward:

```
Api → Infrastructure → Application → Domain
```

A request travels through the layers like this:

```
CreateWalletRequest (Api) → CreateWalletCommand (Application) → CreateWalletHandler
    → Wallet.Create(...) (Domain: enforces the rules)
    → IWalletRepository + IUnitOfWork (implemented in Infrastructure with EF Core)
```

### Design decisions
- **Controllers**, with plural lowercase routes (`api/wallets`)
- **One handler per use case**, no mediator library
- **Request → Command in, Result → Response out:** the HTTP contract is kept separate from the use cases
- **Repository per aggregate + unit of work** for writes; reads go through query services (`IWalletQueries`) that project straight into read models (CQRS-lite)
- **Ledger:** every balance change is an append-only `LedgerEntry` with a positive amount; its type gives the direction. A transfer will be two entries, one per wallet. Entries are never deleted
- **`Money` value object** for amounts that travel with their currency; amounts someone enters are rejected, never rounded
- **Top-up and withdraw are separate operations** (`Wallet.TopUp`, `Wallet.Withdraw`): the wallet changes its balance and returns the ledger entry, and both are saved in one transaction
- **Optimistic concurrency:** a `rowversion` on `Wallets` (an EF shadow property, so the domain stays clean). A conflicting save returns 409 and the client retries
- **Read committed snapshot isolation (RCSI)** on the database: reads use the last committed row version instead of waiting for locks, so a read can't deadlock with a concurrent update
- **Exceptions for broken business rules**, mapped to ProblemDetails (RFC 9457) in one place
- **`TimeProvider`** for the current time, so time-dependent logic is testable
- **Enums travel as text** in JSON (`"EUR"`, never `0`)
- **Contracts are records:** responses positional, requests non-positional with `required` properties (a missing field is a 400, never a silent default)
- **Code style enforced in the build:** explicit types instead of `var`, block bodies for methods

## Tech stack

.NET 10 · ASP.NET Core (controllers) · EF Core 10 · SQL Server 2025 · Serilog · Swagger UI · Docker Compose

## Running locally

**Prerequisites:** Docker Desktop.

1. Create a `.env` file in the repository root (it's git-ignored) with a SQL Server password. It needs 8+ characters with three of: upper case, lower case, digits, symbols:
   ```
   MSSQL_SA_PASSWORD=YourStrong_Passw0rd!
   ```
2. Start everything:
   ```bash
   docker compose up --build -d
   ```
3. Create the tables (needed once per new database, until migrations are applied automatically at startup; uses the user secret from "Running from the IDE" below):
   ```bash
   dotnet ef database update --project src/PulseWallet.Infrastructure --startup-project src/PulseWallet.Api
   ```
4. Open:
   - Swagger UI: http://localhost:5147/swagger
   - Health check: http://localhost:5147/health

Useful commands:

```bash
docker compose logs -f api      # follow the API logs
docker compose down             # stop (data is kept)
docker compose down -v          # stop and delete the database
```

### Running from the IDE
Store the connection string as a user secret, pointing at the SQL Server container:

```bash
dotnet user-secrets set "ConnectionStrings:PulseWallet" "Server=localhost,1433;Database=PulseWallet;User Id=sa;Password=<your password>;Encrypt=True;TrustServerCertificate=True" --project src/PulseWallet.Api
```
