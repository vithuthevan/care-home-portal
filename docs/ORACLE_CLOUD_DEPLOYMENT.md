# Oracle Cloud Infrastructure (OCI) deployment

This guide covers hosting **CarehomeSystem** on an OCI Compute VM with Docker. It applies to:

| Branch | Product | Database migrations |
|--------|---------|---------------------|
| `main` | Baseline back-office (single API project) | `backend/CareHome.Api` |
| `revenue-cycle-v2` | Enhanced revenue cycle (modular monolith) | `backend/CareHome.Data` (startup: `CareHome.Api`) |

**Critical:** Use a **separate SQL database per branch/version**. Never point `main` and `revenue-cycle-v2` at the same database.

See also: `docs/CLOUD_HOSTING_READINESS.md`, `docs/PRODUCTION_DEPLOYMENT.md`, `docs/PRODUCTION_CONFIGURATION.md`.

---

## 1. Choose OCI shape and SQL strategy

| VM type | API Docker image | SQL Server in Docker (`with-sql` profile) |
|---------|------------------|-------------------------------------------|
| **AMD64** (e.g. `VM.Standard.E4.Flex`) | Supported | Possible for **demo/pilot only** — use profile `with-sql`, do not publish port 1433 to the internet |
| **ARM64** (Ampere A1, free tier) | Supported | **Not supported** — SQL Server Linux image is amd64-centric. Use **external SQL** |

**Recommended for production on OCI:** managed or dedicated SQL Server reachable over private networking:

- Second **amd64** VM running SQL Server (Windows or Linux), or
- **Azure SQL** / other hosted SQL Server (connection string over TLS), or
- On-prem SQL with VPN/FastConnect to OCI

Minimum practical RAM: **4 GB** on the app VM if SQL is external; **8 GB+** if SQL runs on the same host.

---

## 2. Network and security (OCI)

1. Create a VCN with a public subnet for the app VM (or private subnet + load balancer).
2. **Security list / NSG ingress:** allow **TCP 22** (SSH) from your IP only; **TCP 80 and 443** from the internet (or from a load balancer).
3. **Do not** open **1433** (SQL) to `0.0.0.0/0`.
4. Terminate **TLS** on the VM (Caddy/nginx) or OCI Load Balancer; the API container listens on `127.0.0.1:8080` only in `docker-compose.prod.yml`.

---

## 3. Prepare the VM

Example: Oracle Linux 8/9 or Ubuntu 22.04 on amd64 or ARM.

```bash
sudo dnf install -y docker-engine docker-compose-plugin git
# Ubuntu: sudo apt install docker.io docker-compose-v2 git

sudo systemctl enable --now docker
sudo usermod -aG docker $USER
# log out and back in
```

Clone the branch you need:

```bash
git clone https://github.com/YOUR_ORG/CarehomeSystem.git
cd CarehomeSystem

# Baseline:
git checkout main

# Enhanced revenue cycle:
git checkout revenue-cycle-v2
```

---

## 4. Create the database and run migrations

Create an empty database and application login (`db_datareader`, `db_datawriter`; `db_ddladmin` only for migration user).

**Backup before every migration** (`docs/BACKUP_RESTORE.md`).

### `main`

```bash
cd backend/CareHome.Api
dotnet ef database update --connection "$CONNECTION_STRING"
```

### `revenue-cycle-v2`

```bash
dotnet ef database update \
  --project backend/CareHome.Data \
  --startup-project backend/CareHome.Api \
  --connection "$CONNECTION_STRING"
```

Confirm latest row in `__EFMigrationsHistory` matches the branch head (see `docs/CLOUD_HOSTING_READINESS.md`).

---

## 5. Configure production secrets on the VM

```bash
cp .env.production.example .env.production
chmod 600 .env.production
# Edit: ConnectionStrings__DefaultConnection, Jwt__Key, Email_*, App__PublicUrl
```

Production rules:

- `ASPNETCORE_ENVIRONMENT=Production` (set in Compose).
- `Database__ApplyMigrations=false` — apply migrations manually (step 4).
- Optional one-time `Seed__AdminEmail` / `Seed__AdminPassword` — remove after first login.

---

## 6. Build and run (production Compose)

```bash
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
curl -sS http://127.0.0.1:8080/health/ready
```

**Optional SQL on same amd64 VM (not for ARM):**

```bash
# Connection string must use host sql and match MSSQL_SA_PASSWORD
docker compose -f docker-compose.prod.yml --profile with-sql up -d
```

Persisted data:

- `carehome-documents` volume — invoice PDFs, Sage exports (back up separately from SQL).

---

## 7. TLS reverse proxy (Caddy example)

```bash
sudo dnf install -y caddy   # or follow caddyserver.com docs
sudo cp infra/oracle/Caddyfile.example /etc/caddy/Caddyfile
# Edit domain and email
sudo systemctl enable --now caddy
```

Users hit `https://your-domain` → Caddy → `127.0.0.1:8080` (SPA + `/api` same origin).

---

## 8. Alternative: publish without Docker

On the VM (requires .NET 10 SDK/runtime and Node 22 for build):

```bash
# From repo root (PowerShell on Windows, or use pwsh on Linux):
./scripts/Publish-CareHome.ps1 -OutputDir /opt/carehome-api

export ASPNETCORE_ENVIRONMENT=Production
# set ConnectionStrings__DefaultConnection, Jwt__Key, etc.
dotnet /opt/carehome-api/CareHome.Api.dll
```

Use **systemd** unit + Caddy as above. Same migration and secret rules apply.

---

## 9. GitHub Actions CI/CD

| Workflow | When it runs | What it does |
|----------|----------------|--------------|
| **CI** (`.github/workflows/ci.yml`) | Pull requests and pushes to `main`, `master`, `develop`, `revenue-cycle-v2` | Calls shared quality gates (no deploy). |
| **Quality gates** (`.github/workflows/quality-gates.yml`) | Reused by CI and deploy | .NET build; EF pending-model check; `CareHome.Api.dll --apply-migrations` against CI SQL Server; full test suite (including SQL integration tests); Angular build and unit tests; `Dockerfile.prod` build smoke test. |
| **Deploy Oracle** (`.github/workflows/deploy-oracle.yml`) | Push to `revenue-cycle-v2` or **workflow_dispatch** | Runs quality gates → builds production SPA + API tarball → uploads artifact → SSH to VM → `apply-database-migrations-on-host.sh` → restarts `carehome-api` → `/health/live` check. |

**Deploy secrets** (repository **Settings → Secrets and variables → Actions**):

| Secret | Purpose |
|--------|---------|
| `OCI_HOST` | VM public IP or hostname |
| `OCI_USER` | SSH user (e.g. `ubuntu`) |
| `OCI_SSH_KEY` | Private key for deploy (PEM contents) |

Optional: configure a GitHub **Environment** named `production` on the deploy job for approval gates and deployment history.

Deployments never run if quality gates fail. Database migrations on the server use the same `--apply-migrations` entrypoint as `scripts/apply-database-migrations-on-host.sh` (systemd environment for the live connection string).

---

## 10. Branch-specific checklist

### `main`

- [ ] Dedicated database (e.g. `CareHome_Main`)
- [ ] Migrations from `CareHome.Api`
- [ ] `docker compose -f docker-compose.prod.yml build` (API-only backend tree is fine)

### `revenue-cycle-v2`

- [ ] **Different** database (e.g. `CareHome_V2`)
- [ ] Migrations from `CareHome.Data`
- [ ] Fixed multi-project `Dockerfile` / `Dockerfile.prod` (copies full `backend/`)

---

## 11. Operations

| Task | Action |
|------|--------|
| Health | `GET /health/live`, `GET /health/ready` |
| Upgrade | SQL backup → `docker compose build` → `database update` → `up -d` |
| Documents | Snapshot volume or `robocopy`/`rsync` of `DocumentStorage__RootPath` |
| Logs | `docker compose -f docker-compose.prod.yml logs -f api` |
| Dev/demo locally | `docker compose up` (Development; separate web container) — **not** for public OCI exposure |

---

## 12. What not to do on OCI

- Do not expose `docker-compose.yml` dev stack (SQL on `14333`, Development JWT, auto-migrate) to the public internet.
- Do not share one database between `main` and `revenue-cycle-v2`.
- Do not rely on SQL Server in Docker on **ARM** Ampere instances.
