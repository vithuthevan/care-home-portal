# Care Home Back-Office Management System

MVP for companies, care homes, clients, funding contracts, effective-dated rates, billing, invoices, PDFs, email, credit notes, miscellaneous CSV charges, reports, Sage50 CSV export, users, and audit.

## Stack

- Frontend: Angular 22, standalone components, Reactive Forms, HttpClient, dev proxy `/api`
- Backend: ASP.NET Core / .NET 10 Web API, EF Core, SQL Server / LocalDB
- Auth: ASP.NET Core Identity + JWT
- PDF: QuestPDF · Excel: ClosedXML

## Project structure

```
backend/CareHome.Api/     API, EF models, migrations, billing/email/export
frontend/care-home-web/   Angular SPA
docs/                     Architecture, operations, demo, and pilot documentation (see docs/README.md)
```

## Prerequisites

.NET 10 SDK, SQL Server LocalDB (or SQL Server), Node.js.

**Or** Docker Desktop, to run the full stack with one command.

## Run with Docker

From the repository root:

```powershell
docker compose up -d --build
```

- Frontend: http://localhost:4200
- API: http://localhost:5092
- Health: http://localhost:5092/health/live and http://localhost:5092/health/ready
- SQL Server: `localhost,14333` / database `CareHomeDb` / user `sa` (password in `.env.example`)

Development login after first start: `admin@localhost` / `DevAdmin!12345`.

Optional: copy `.env.example` to `.env` to change the SQL password. Email stays simulated (`Email:Mode=Development`). SMTP is not required locally.

```powershell
docker compose logs -f
docker compose stop
docker compose down
```

## Host on Oracle Cloud (OCI)

Production on a Compute VM: `docker-compose.prod.yml`, `Dockerfile.prod`, TLS via Caddy/nginx. Use a **dedicated database per branch** (`main` vs `revenue-cycle-v2`). See `docs/ORACLE_CLOUD_DEPLOYMENT.md`.

## Host on Azure

Recommended Production host: App Service (API + Angular same origin) + Azure SQL. See `docs/AZURE_HOSTING.md`.

```powershell
az login
.\scripts\Deploy-Azure.ps1 -ResourceGroup rg-carehome -Location uksouth -AppName carehome-pilot
```

## Configure database

Edit `backend/CareHome.Api/appsettings.json` → `ConnectionStrings:DefaultConnection`.

## Apply migrations

```powershell
cd backend\CareHome.Api
dotnet restore
dotnet ef database update
```

Do not use `EnsureCreated`. Do not edit already-applied migrations.

## Run backend

```powershell
cd backend\CareHome.Api
dotnet watch run --launch-profile http
```

http://localhost:5092 — `dotnet watch` rebuilds and restarts the API when C# files change.

## Run frontend

```powershell
cd frontend\care-home-web
npm install
npm start
```

http://localhost:4200 — `npm start` runs `ng serve --hmr` with automatic rebuild and browser refresh on file changes.

For Docker UI development with bind-mounted sources (Windows-friendly polling):

```powershell
docker compose -f docker-compose.yml -f docker-compose.dev.yml up sql api web
```

The default `docker compose up` bakes API and UI at **build** time. Use the dev compose file above (or host `dotnet watch` + `npm start`) so edits apply without rebuilding images.

## Development login

After migrations, Development seed creates:

- Email: `admin@localhost`
- Password: `DevAdmin!12345`

Override with environment variables `Seed__AdminEmail` / `Seed__AdminPassword`. Production config leaves these empty.

## SMTP

Default `Email:Mode` = `Development` (simulate + log). For real send set `Email:Mode` = `Smtp` and SMTP settings via environment variables. Do not commit credentials.

## PDF storage

`App_Data/documents` unless `DocumentStorage:RootPath` is set.

## Sage export

CSV files under the document store. Mapping is provisional — see `docs/SAGE50_EXPORT.md`.

## Main workflow

1. Configure Company  
2. Configure Care Home  
3. Configure Funding Authority  
4. Configure Invoice Category  
5. Configure Nominal Code  
6. Create Client  
7. Create Funding Contract  
8. Add Rate  
9. Preview Billing  
10. Generate Invoice  
11. Download / send PDF  
12. Credit / reinvoice if needed  
13. Update Payment Status  
14. Export to Sage  

New product users: start with **[docs/USER_GUIDE.md](docs/USER_GUIDE.md)** (A–Z section walkthrough).

Client demo presenters: **[docs/demo/CLIENT_DEMO_A_TO_Z_WALKTHROUGH.md](docs/demo/CLIENT_DEMO_A_TO_Z_WALKTHROUGH.md)** and **[docs/demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md](docs/demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md)**.

Engineers: start learning with **[docs/LEARNING_GUIDE.md](docs/LEARNING_GUIDE.md)**. Full doc index: **[docs/README.md](docs/README.md)**.

Deep-dive notes from architecture review:

- [Auth & multi-tenancy](docs/LEARNING_NOTES_01_AUTH_TENANCY.md)
- [Billing engine](docs/LEARNING_NOTES_02_BILLING.md)
- [Concurrency & idempotency](docs/LEARNING_NOTES_03_CONCURRENCY.md)
- [Architect interview answers](docs/ARCHITECT_INTERVIEW_ANSWERS.md)
