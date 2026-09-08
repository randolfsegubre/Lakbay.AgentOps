# Lakbay.AgentOps

Backend-for-frontend for [Lakbay.AgentDesktop](../Lakbay.AgentDesktop): aggregates
`Lakbay.AvailabilityApi`'s catalog into an agent-shaped "offer" view, proxies booking
confirmations to `Lakbay.Booking`, logs calls, and pushes live availability changes to
connected desktop clients.

See [ADR-0021](../Lakbay.Docs/docs/adr/ADR-0021-agent-channel-new-repos.md) for why this
repo exists and [ADR-0025](../Lakbay.Docs/docs/adr/ADR-0025-abp-hangfire-redis-signalr-agentops.md)
for the ABP/Hangfire/Redis/SignalR stack decisions.

## Stack

ABP Framework (layered, `aspnet-core/`) + Hangfire (background jobs) + Redis (offer cache)
+ SignalR (`AgentAvailabilityHub`, live push) + two databases: SQL Server (ABP's own
identity/Hangfire storage) and Oracle
([ADR-0024](../Lakbay.Docs/docs/adr/ADR-0024-oracle-call-log-polyglot-persistence.md),
call-log/CRM read side — framed as integrating with a pre-existing on-prem system).

## Local setup

```powershell
cd aspnet-core
dotnet build Lakbay.AgentOps.slnx        # 0 warnings, 0 errors

# One-time: create the Hangfire job-storage database (Hangfire installs its own
# schema into it on first run, but the database itself must already exist)
sqlcmd -S "(localdb)\mssqllocaldb" -Q "IF DB_ID('AgentOpsHangfire') IS NULL CREATE DATABASE AgentOpsHangfire" -C

cd src\Lakbay.AgentOps.DbMigrator
dotnet run                                # sets up ABP's own SQL Server database

cd ..\Lakbay.AgentOps.HttpApi.Host
dotnet run --urls http://localhost:5250
```

**Secrets** — set locally via `dotnet user-secrets` in `Lakbay.AgentOps.HttpApi.Host`
(never in `appsettings.json`):

```powershell
dotnet user-secrets set "StringEncryption:DefaultPassPhrase" "<a real random value>"
dotnet user-secrets set "Redis:Configuration" "127.0.0.1:6379,password=<your local Redis password>"
dotnet user-secrets set "Oracle:ConnectionString" "User Id=agentops;Password=<...>;Data Source=localhost:1521/FREEPDB1"
```

`StringEncryption:DefaultPassPhrase` matters even locally: every ABP app scaffolded from
the default template ships the *same* hardcoded passphrase in `appsettings.json` unless
you change it — a real, easy-to-miss issue caught and fixed during this repo's build.

**Redis**: this session used a portable `redis-server.exe` (no Docker, no admin install —
see `Lakbay/.tools/redis`), started with a generated password and bound to `127.0.0.1`
only. `Volo.Abp.Caching.StackExchangeRedis` is wired in `AgentOpsHttpApiHostModule`.

**Oracle**: requires an elevated, one-time install — see
`Lakbay/.tools/oracle/install-oracle-elevated.ps1`. Until that's run, endpoints that
don't touch call-logging (the offer aggregation, once `Lakbay.AvailabilityApi` is up)
work fine; anything hitting `ICallRecordRepository` will fail with a clear
`InvalidOperationException` naming the missing configuration, not a confusing raw EF
exception.

## Verified live (2026-09-08)

- `GET /api/agent-offer/{destinationCode}` — real route, real Redis-cached GraphQL
  aggregation against `Lakbay.AvailabilityApi` (fails cleanly with a real
  `ConnectToTcpHostAsync` error when that service isn't running — confirmed this is an
  infrastructure gap, not a code bug, by reading the actual exception).
- `POST /api/bookings/confirm` — proxies to a real running `Lakbay.Booking`:
  - First confirm on a slot with capacity: `200 {"success":true,"bookingId":"...","paymentStatus":"PendingInvoice",...}`
  - Confirm on an exhausted slot: `200 {"success":false,"message":"This slot is no longer available."}` (correctly mapped from Booking's `409`)
  - Verified this holds even across app restarts, since availability is real, persisted state
- `/hangfire` dashboard correctly returns `401` unauthenticated (ADR-0025's "auth-gated, never open to anyone" requirement) — confirmed live, not just by reading the code.
- Hangfire's own SQL Server storage installs and runs a real background server (verified via startup logs, once the `AgentOpsHangfire` database was created).

## Security note: rotate the seeded admin password before any real deployment

ABP's own host database seeder creates a default `admin` / `1q2w3E*` account on first
run — this is standard, well-documented ABP framework behavior, not something specific
to this repo, but it's exactly the kind of well-known default credential that must be
rotated before this ever runs anywhere reachable. The one place that password appeared
in this repo (`Lakbay.AgentOps.HttpApi.Client.ConsoleTestApp/appsettings.json`, a local
dev tool for exercising the HTTP API) has been moved to `dotnet user-secrets` rather than
left in a committed file.

## Verified live (2026-09-09)

- `GET /api/agent-offer/{destinationCode}` hit a genuinely live
  `Lakbay.AvailabilityApi` for the first time — real Coron accommodation/
  package/activity data returned, confirmed cached in Redis
  (`redis-cli keys` showed `agent-offer:<destinationId>`).
- A real booking confirmed end-to-end through `POST /api/bookings/confirm`
  to a live `Lakbay.Booking` (`coron-island-hopping`, 7 days out) —
  `200 {"success":true,"bookingId":"...","paymentStatus":"PendingInvoice"}`.
- **A real bug found and fixed**: the EF Core version pin below only
  covered the umbrella `Microsoft.EntityFrameworkCore` package, not
  `Microsoft.EntityFrameworkCore.Relational` — the shipped host binary was
  still 10.0.9, causing a `FileNotFoundException` in the call-logging
  Hangfire job instead of the clean error this doc used to claim. Fixed by
  pinning `Microsoft.EntityFrameworkCore.Relational` 10.0.11 explicitly in
  `Lakbay.AgentOps.HttpApi.Host.csproj`; re-verified live — the job now
  fails with the correct `ORA-12541: no listener at 127.0.0.1:1521` (Oracle
  not installed yet is the only remaining gap in that path, not a code bug).

## Known gaps

- Oracle AI Database Free is not yet installed locally, so
  `ICallRecordRepository` calls still fail — cleanly now, with the
  documented `ORA-12541` connectivity error, not a confusing assembly
  exception. See `Lakbay/.tools/oracle/install-oracle-elevated.ps1`.
- `AgentDesktopController.ConfirmBooking` generates a fresh `CallId` for call-logging
  rather than correlating to a real active call — `Lakbay.AgentDesktop` doesn't yet pass
  its `IncomingCallViewModel.CallId` through to the booking request (see that repo's own
  known-gaps list).
- No automated tests for this repo yet.
