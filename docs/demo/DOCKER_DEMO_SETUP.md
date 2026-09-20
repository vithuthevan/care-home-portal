# Local Docker demo setup (Care Home)

Use this guide to run the **client demo** on your machine with Docker Compose. This stack is **Development only** — not production, not Azure, and not real SMTP.

For the live demo script and operator checklist, see [CLIENT_DEMO_OPERATOR_RUNBOOK.md](CLIENT_DEMO_OPERATOR_RUNBOOK.md).

---

## 1. Prerequisites

| Requirement | Notes |
|-------------|--------|
| **Windows 10/11** | 64-bit |
| **Docker Desktop** | Includes Docker Engine and Compose v2 (`docker compose`) |
| **RAM / disk** | Allow ~4 GB RAM for SQL + API + Angular dev server; several GB free disk for images |
| **Ports free** | `4200`, `5092`, `14333` (see [Port requirements](#9-port-requirements)) |
| **Repository** | Clone or open the repo; all commands run from the **root** (folder containing `docker-compose.yml`) |

Optional:

- Copy `.env.example` or `.env.demo.example` to `.env` only if you need to override the SQL SA password. **Defaults work without `.env`.**

---

## 2. Docker Desktop setup (Windows)

1. Install [Docker Desktop for Windows](https://docs.docker.com/desktop/setup/install/windows-install/) if it is not installed.
2. Enable **WSL 2** backend when prompted (recommended).
3. Start **Docker Desktop** and wait until the whale icon shows **Docker Desktop is running**.
4. Open PowerShell and confirm:

```powershell
docker version
docker compose version
```

You should see both **Client** and **Server** under `docker version`. If Server is missing, the daemon is not running — start Docker Desktop and retry.

---

## 3. Starting Docker

- Launch **Docker Desktop** before any `docker compose` commands.
- After sleep or reboot, verify the daemon: `docker info` (should not error).

---

## 4. Starting the application

From the repository root:

```powershell
cd C:\path\to\CarehomeSystem
docker compose up -d --build
```

**First run** can take several minutes (pull SQL Server image, `npm install` in the web image, .NET publish for API).

### What starts

| Service | Container name | Host port | Purpose |
|---------|----------------|-----------|---------|
| `sql` | `carehome-sql` | **14333** → 1433 | SQL Server 2022 (Developer), data in volume `carehome_carehome-sql-data` |
| `api` | `carehome-api` | **5092** → 8080 | ASP.NET Core API, `ASPNETCORE_ENVIRONMENT=Development` |
| `web` | `carehome-web` | **4200** → 4200 | Angular dev server; proxies `/api` and `/health` to `http://api:8080` |

Startup order: SQL becomes healthy → API applies migrations and becomes healthy → web starts.

### Default SQL password

Compose uses `MSSQL_SA_PASSWORD` from `.env` if present; otherwise:

`CareHomeDevSql2022@`

(See `.env.example` and `.env.demo.example` for placeholders — do not commit real secrets.)

### Demo login (Development seed)

After the API starts, the PlatformAdmin user is seeded from `appsettings.Development.json`:

- **Email:** `admin@localhost`
- **Password:** `DevAdmin!12345`

**UI:** http://localhost:4200  
**API (direct):** http://localhost:5092

---

## 5. Health verification

Wait until all services are healthy (web can take **30–90 seconds** after API is healthy while `ng serve` compiles).

```powershell
docker compose ps
```

Expected: `carehome-sql`, `carehome-api`, and `carehome-web` **Up** and **(healthy)** (web may briefly show `health: starting`).

### API

```powershell
curl.exe -s http://localhost:5092/health/live
curl.exe -s http://localhost:5092/health/ready
```

Both should return JSON with `"status":"Healthy"` and HTTP **200**.

### Frontend and API proxy

```powershell
curl.exe -s -o NUL -w "frontend:%{http_code}\n" http://localhost:4200/
curl.exe -s -o NUL -w "proxied-ready:%{http_code}\n" http://localhost:4200/health/ready
```

Expect **200** for both.

### SQL database

```powershell
docker exec carehome-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "CareHomeDevSql2022@" -Q "SELECT name FROM sys.databases WHERE name='CareHomeDb'" -C -b
```

Expect one row: `CareHomeDb`. Migrations run automatically when `Database__ApplyMigrations=true` (set in `docker-compose.yml`).

### Login smoke test (PowerShell)

```powershell
Invoke-RestMethod -Uri http://localhost:5092/api/auth/login -Method POST `
  -ContentType 'application/json' `
  -Body (@{ email = 'admin@localhost'; password = 'DevAdmin!12345' } | ConvertTo-Json)
```

Expect a `token` and `roles` containing `PlatformAdmin`.

### Logs (only when troubleshooting)

```powershell
docker compose logs sql --tail 50
docker compose logs api --tail 50
docker compose logs web --tail 50
```

---

## 6. Stopping the application

Stop containers but **keep** database and document data:

```powershell
docker compose down
```

Start again later with `docker compose up -d` (add `--build` if you changed Dockerfiles or dependencies).

---

## 7. Clean reset (local demo volumes only)

To wipe **only** this Compose project’s data and get a fresh database and document store:

```powershell
docker compose down -v
docker compose up -d --build
```

### Volumes affected

Only volumes defined in this project’s `docker-compose.yml`:

- `carehome_carehome-sql-data` (SQL data)
- `carehome_carehome-documents` (uploaded documents)

Docker names them with the project prefix `carehome_`.

### Safety — never use `docker compose down -v` for

- Azure-hosted environments
- Production or staging
- Shared team databases
- Any SQL instance that is **not** the `carehome-sql` container on your machine

Also **do not** run broad cleanup on this machine for the demo:

- `docker system prune`
- `docker volume prune`
- `docker container prune`

Those can remove **unrelated** images, containers, or volumes. Operate only on the `carehome` stack.

---

## 8. Troubleshooting

| Symptom | What to try |
|---------|-------------|
| `error during connect` / no Server in `docker version` | Start Docker Desktop; wait until running |
| Port already in use | Free `4200`, `5092`, or `14333`, or stop the conflicting app (do not stop unrelated Docker projects unless you own them) |
| `sql` unhealthy | `docker compose logs sql`; wait longer on first start; check SA password matches in `.env` and compose |
| `api` restart loop | `docker compose logs api`; often SQL not ready or wrong password |
| `api` unhealthy | Check `/health/ready` — usually database not reachable; verify `sql` is healthy |
| `web` build failed | Ensure current `frontend/care-home-web/Dockerfile` uses `node:22.22-bookworm-slim` and `npm install` (not `npm ci` with an out-of-sync lockfile) |
| Frontend 502 / API errors in browser | Confirm `carehome-api` healthy; proxy is `proxy.conf.docker.json` → `http://api:8080` |
| Login fails in UI but API works | Use Development credentials above; clear site data for `localhost:4200` |
| Slow first page load | Normal: Angular `ng serve` inside container |

Restart a single service without wiping data:

```powershell
docker compose restart api
docker compose restart web
```

---

## 9. Port requirements

| Port | Service | Access |
|------|---------|--------|
| **4200** | Web (Angular) | Browser: http://localhost:4200 |
| **5092** | API | http://localhost:5092 (Swagger/health/login) |
| **14333** | SQL Server | Host tools only (optional); API uses hostname `sql` inside the network |

Check Windows port use:

```powershell
netstat -ano | findstr ":4200 :5092 :14333"
```

No output usually means ports are free.

---

## 10. Common Docker errors

| Message | Meaning / fix |
|---------|----------------|
| `The system cannot find the file specified` (Docker pipe) | Docker Desktop not running |
| `port is already allocated` | Another process or container uses that host port |
| `npm ci` / lockfile sync errors during **web** build | Use the repo’s current web Dockerfile (`npm install`, Node 22.22+) |
| `Login failed for user 'sa'` | `MSSQL_SA_PASSWORD` mismatch between `sql` and `api` — set both via `.env` or use defaults |
| `no such file` for `sqlcmd` in healthcheck | Image updated; compose healthcheck tries `mssql-tools18` then legacy path — ensure you use the compose file from this repo |
| PowerShell `curl` fails | Use `curl.exe` or `Invoke-RestMethod` (PowerShell aliases `curl` to `Invoke-WebRequest`) |

---

## Quick reference — start demo tomorrow

```powershell
# 1. Start Docker Desktop (GUI) and wait until running

# 2. From repo root
cd C:\Users\HP\Downloads\CarehomeSystem

# 3. Optional fresh DB/documents (LOCAL DEMO ONLY)
# docker compose down -v

# 4. Start stack
docker compose up -d --build

# 5. Wait ~1–2 minutes, then verify
docker compose ps
curl.exe -s http://localhost:5092/health/ready
curl.exe -s -o NUL -w "web:%{http_code}\n" http://localhost:4200/

# 6. Open browser
# http://localhost:4200
# Login: admin@localhost / DevAdmin!12345
```

To stop after the demo: `docker compose down` (keeps data) or `docker compose down -v` (local reset only).
