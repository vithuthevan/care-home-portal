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
docs/                     Architecture and learning notes
```

## Prerequisites

.NET 10 SDK, SQL Server LocalDB (or SQL Server), Node.js.

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
dotnet run --launch-profile http
```

http://localhost:5092

## Run frontend

```powershell
cd frontend\care-home-web
npm install
npm start
```

http://localhost:4200

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

Start learning with **[docs/LEARNING_GUIDE.md](docs/LEARNING_GUIDE.md)**.
