# CoreERP

CoreERP is a modular monolith mini ERP for small and medium businesses built with:

- Backend: .NET 8, ASP.NET Core Web API, EF Core, SQL Server
- Frontend: Angular 17 standalone components with Angular Material
- Security: ASP.NET Core Identity, JWT, refresh tokens, permission policies
- Reporting: ClosedXML and QuestPDF
- Jobs: Hangfire
- Logging: Serilog

## Current Verification Status

Backend stabilization completed in-repo:

- API startup is now provider-aware: SQL Server remains the default runtime database, while SQLite is supported for integration testing
- A committed SQL Server baseline migration was added under `src/ERP.Infrastructure/Persistence/Migrations`
- A design-time `ErpDbContextFactory` was added so `dotnet ef` can target the current model consistently
- Hangfire and HTTPS redirection are now configuration-driven, which keeps Docker/local HTTP flows working and allows backend integration tests to run without SQL Server-specific job storage
- Integration tests now cover login, product creation, purchase order submit/approve/receive, sales order submit/approve, sales invoice posting, stock deduction, low stock alert generation, and frontend-facing API smoke endpoints

What was already in place and preserved:

- Domain model for branches, products, customers, suppliers, purchase orders, sales orders, invoices, payments, returns, inventory, approvals, alerts, and audit logs
- Application services for master data, transactions, approvals, dashboard, reports, alerts, and audit log queries
- API controllers for auth, admin, master data, purchasing, sales, inventory, reports, dashboard, alerts, and audit logs
- Frontend shell, auth flow, dashboard, master data, transaction pages, reports, users, roles/workflow, alerts, and audit log pages
- Demo seed strategy and startup bootstrap path

Environment limitation in this workspace:

- The .NET 8 SDK is not installed in this session, so `dotnet build`, `dotnet test`, `dotnet ef`, and live API execution could not be run here
- Docker is also unavailable in this session, so Docker Compose could only be prepared, not executed

## Solution Structure

```text
src/
  ERP.Api
  ERP.Application
  ERP.Domain
  ERP.Infrastructure
  erp-web
tests/
  ERP.UnitTests
  ERP.IntegrationTests
```

## Key Business Flows

- Purchase orders: draft, edit, submit for approval, approve from dashboard, then invoice/receive into stock
- Sales orders: draft, edit, submit for approval, approve from dashboard, then invoice and reduce stock
- Inventory: branch balances, movement ledger, adjustments, transfers, low stock monitoring
- Finance: purchase invoices, sales invoices, customer receipts, supplier payments, returns
- Governance: users, roles, permissions, approval rules, alerts, audit logs
- Reporting: sales, purchases, stock valuation, stock movement, low stock, receivables, payables with Excel/PDF export endpoints

## Local Run

### Option 1: Docker Compose

From the repository root:

```bash
docker compose up --build
```

Then open:

- Frontend: http://localhost:4200
- API Swagger: http://localhost:8080/swagger
- Hangfire: http://localhost:8080/hangfire

### Option 2: Run SQL Server + API + frontend locally

Requirements:

- .NET 8 SDK
- SQL Server 2022, SQL Server Express, or LocalDB
- Node.js 20+

SQL Server connection defaults:

- Docker/standard SQL Server: `src/ERP.Api/appsettings.json`
- LocalDB development: `src/ERP.Api/appsettings.Development.json`

Restore and build:

```bash
dotnet restore ERP.sln
dotnet build ERP.sln
```

Apply EF Core migrations:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/ERP.Infrastructure --startup-project src/ERP.Api
```

Run the API:

```bash
dotnet run --project src/ERP.Api
```

Run the Angular frontend in a second terminal:

```bash
cd src/erp-web
npm install
npm start
npm run build
```

The frontend loads its API base URL from `src/erp-web/src/assets/app-config.json`.

### Option 3: Run tests locally

Unit tests:

```bash
dotnet test tests/ERP.UnitTests/ERP.UnitTests.csproj
```

Integration tests:

```bash
dotnet test tests/ERP.IntegrationTests/ERP.IntegrationTests.csproj
```

The integration suite uses a SQLite in-memory database, disables Hangfire, seeds the real application data, and exercises the API through `WebApplicationFactory`.

### Option 4: Create future migrations

If the domain model changes after this baseline:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add <MigrationName> --project src/ERP.Infrastructure --startup-project src/ERP.Api --output-dir Persistence/Migrations
```

## Seeded Accounts

- `admin / Admin123!`
- `manager / Manager123!`
- `branchuser / Branch123!`

## Verified In This Workspace

- `npm.cmd install` in `src/erp-web`
- `npm.cmd run build` in `src/erp-web`
- Frontend route-to-endpoint alignment was checked against the current API controllers
- Backend runtime, migration, and integration-test files were prepared for immediate execution in a proper .NET 8 environment

## Not Yet Verifiable Here

- `dotnet build`, `dotnet test`, `dotnet ef database update`, and API runtime verification could not be executed in this workspace because the .NET SDK is not installed in this machine session
- `docker compose up --build` could not be executed here because Docker is not installed in this machine session

## Tradeoffs And Next Improvements

- The committed baseline migration is hand-authored because `dotnet ef migrations add` could not be executed without the SDK in this session; once the SDK is available, running the migration commands above is the final validation step
- Frontend forms are functional and connected to live endpoints, but they are intentionally streamlined compared to a fully polished enterprise workflow studio
- Approval actions are available from the dashboard and admin workflow screens; a dedicated approval inbox page would still improve high-volume operations
- Once a .NET 8 environment is available, the next recommended pass is to run the full build/test pipeline and, if desired, add a generated model snapshot beside the committed baseline migration
