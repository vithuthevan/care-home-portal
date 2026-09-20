# Demo Pre-Flight Checklist

**Date:** 15 September 2026  
**Environment:** Local Docker Compose + Development only  
**Run:** Same day, **before** the client joins

---

## Environment

- [ ] Docker Desktop is running
- [ ] Demo stack started: `docker compose down -v` then `docker compose up -d --build`
- [ ] `docker compose ps` — `carehome-sql` and `carehome-api` show **healthy**
- [ ] `GET http://localhost:5092/health/live` → Healthy
- [ ] `GET http://localhost:5092/health/ready` → Healthy
- [ ] http://localhost:4200 loads the **login page**
- [ ] `ASPNETCORE_ENVIRONMENT=Development` (Docker Compose default — do not change)
- [ ] Browser site data cleared for `localhost:4200` (or fresh profile); no stale `carehome.auth` JWT
- [ ] Demo run from a **known git commit** (no surprise uncommitted WIP)
- [ ] Terminal windows minimized unless needed for recovery

---

## Users

- [ ] **PlatformAdmin** works for setup: `admin@localhost` (password confirmed — **not shown to client**)
- [ ] **Demo Care Group** organisation exists and is **Active**
- [ ] **Existing Organisation** is **Inactive**
- [ ] **TenantAdmin** (`demo-admin@example.com`) password change **already completed**
- [ ] TenantAdmin lands on `/dashboard` with **Demo Care Group** in header
- [ ] **ReadOnly** user exists if showing permissions (`demo-viewer@example.com`)
- [ ] Optional **Administrator** user exists if showing accountant persona

---

## Data

All data in **Demo Care Group** only — not tenant Id=1.

- [ ] Company: **Demo Care Ltd**
- [ ] Care homes: **River View House** (`RIVER01`), **Meadow Court** (`MEADOW02`) if used
- [ ] Funding authorities: Anytown Council, Private Funder Ltd
- [ ] Nominal code `4000` and invoice template with **fictional** bank details
- [ ] At least **2 fictional residents** (e.g. Alex Morgan, Jordan Blake) with active contracts and rates
- [ ] Billing period rehearsed — admission dates and rates cover the demo month
- [ ] At least **2 finalized invoices** — one **Paid**, one **Not paid**
- [ ] At least **1 credit note** (e.g. `CN-0001` against unpaid invoice)
- [ ] Report data returns rows (outstanding invoice visible)
- [ ] No real customer names, NHS numbers, addresses, or funder emails
- [ ] No Sovereign Care Homes / Care Pro / old UAT leftovers

---

## Functional

Run as **TenantAdmin** unless noted.

- [ ] Login succeeds without forced password change
- [ ] Dashboard loads
- [ ] Resident profile opens; **Funding** tab shows contract and rate
- [ ] Billing **preview** returns lines for rehearsed period
- [ ] Invoice list shows pre-built invoices with correct amounts
- [ ] **PDF download** opens valid PDF (`%PDF` header)
- [ ] Credit note PDF downloads (if credit note created)
- [ ] Payment status toggle works on a Not paid invoice
- [ ] Credit note visible and linked to correct invoice
- [ ] At least one **report** runs without error
- [ ] Report **export** downloads a file (optional)
- [ ] `/audit` lists recent demo actions
- [ ] PlatformAdmin gets **403** on `/companies` (expected — optional sanity check)

---

## Email

- [ ] `Email:Mode` is **Development** (default — no SMTP configured)
- [ ] Invoice **send** returns success with simulation disclaimer in UI
- [ ] API logs show `EMAIL SIMULATED` if checked: `docker compose logs api | Select-String "EMAIL SIMULATED"`
- [ ] Recipient addresses are fictional only (`@example.com`, `demo@localhost`)
- [ ] Presenter script ready: *"Email is simulated — no message leaves this machine"*
- [ ] **No real SMTP** configured for demo
- [ ] **No real customer emails** used

---

## Presentation

- [ ] Browser zoom and resolution suitable for screen share
- [ ] Browser bookmarks/history clean — no unrelated tabs
- [ ] No personal data visible
- [ ] DevTools / React/Angular debug panels closed
- [ ] No source code or IDE visible unless client asks
- [ ] Password manager ready (not displayed on screen)
- [ ] Billing/Sage disclaimer prepared (*pending finance sign-off*)
- [ ] End-to-end rehearsal completed once with timing

---

## Demo recovery plan

If something goes wrong **during** the demo, use these steps. All commands are **limited to the local Docker demo stack**.

### Quick diagnostics

| Symptom | First check |
|---------|-------------|
| Blank page / API errors | `docker compose ps` — are all containers up and healthy? |
| 401 / redirect to login | JWT expired or wrong user — log out; clear `localStorage` key `carehome.auth` |
| 403 on business screens | Logged in as **PlatformAdmin** — switch to **TenantAdmin** |
| Stuck on password change | Complete change once, or use pre-configured TenantAdmin |
| Billing empty / errors | Missing rate, template, or contract; check admission dates |
| `ALREADY_FULLY_BILLED` | Period already invoiced — open existing invoice instead of regenerating |
| PDF fails | Check API logs; verify `carehome-documents` volume |
| Email send fails | Confirm `Email:Mode=Development` and `ASPNETCORE_ENVIRONMENT=Development` |

### Restart API only

```powershell
docker compose restart api
```

Wait for healthcheck, then refresh the browser.

### Restart frontend only

```powershell
docker compose restart web
```

Wait ~30–90 seconds for `ng serve` to be ready, then refresh.

### Check logs

```powershell
docker compose logs -f api
docker compose logs -f sql
docker compose logs api | Select-String "EMAIL SIMULATED|error|exception" -CaseSensitive:$false
```

### Verify database connectivity

```powershell
curl http://localhost:5092/health/ready
```

If **Unhealthy**, SQL may still be starting — wait 1–2 minutes or check `docker compose logs sql`.

### Switch to pre-built data (avoid live billing)

If live invoice generation fails mid-demo:

1. Navigate to `/invoices` and open **pre-built** INV-0001 / INV-0002.
2. Continue with PDF, payment, credit note, and reports from existing data.
3. Explain: *"We pre-generated August billing during setup — same outcome as live generation."*

### Full demo reset (local only)

**Warning:** Destroys all local demo SQL and document data. Use only on the isolated demo machine.

```powershell
docker compose down -v
docker compose up -d --build
```

Then repeat setup from `CLIENT_DEMO_SCRIPT.md` (PlatformAdmin → create org → deactivate Existing Organisation → TenantAdmin → demo data). Allow 15–30 minutes for full re-setup.

**Do not** run `down -v` against Azure SQL, shared LocalDB, or any non-demo database.

### Partial reset (database only — unusual)

```powershell
docker compose down
docker volume rm carehome_carehome-sql-data
docker compose up -d
```

Prefer full `down -v` for predictable state.

### Login rate limit

Login is limited to **10 attempts per minute per IP**. If locked out, wait one minute and retry with the correct password.

### Escalation path

| Severity | Action |
|----------|--------|
| Transient UI glitch | Refresh browser; restart `web` |
| API 500 on one action | Restart `api`; use pre-built invoice/report |
| Database corrupt / inconsistent | Full local reset (`down -v`) + break while re-setup |
| Cannot recover in 5 minutes | Acknowledge; reschedule technical segment; show PDF/report exports offline if available |

---

## Related documents

| Document | Use |
|----------|-----|
| `CLIENT_DEMO_SCRIPT.md` | Demo narrative and startup procedure |
| `DEMO_ENVIRONMENT_VARIABLES.md` | Configuration reference |
| `CLIENT_DEMO_ENVIRONMENT_SETUP.md` | Extended troubleshooting table |
