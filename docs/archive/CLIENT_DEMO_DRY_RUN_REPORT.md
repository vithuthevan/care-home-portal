# Client Demo Dry Run Report

**Final live verification:** 16 September 2026  
**Reviewer scope:** Read-only — no code, data, configuration, or infrastructure changes  
**Environment:** Local Docker Compose (`ASPNETCORE_ENVIRONMENT=Development`), containers reported running by operator  
**References:** `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md`, `../demo/DEMO_DATA_PREPARATION_GUIDE.md`, `../archive/CREDIT_NOTE_INVESTIGATION.md`

---

## Final Verdict

## 🔴 NOT READY

The Docker stack is **up and healthy**, but the **demo tenant and fictional dataset are not present**. The database reflects a **post-migration greenfield** state (tenant Id=1 only, zero clients/invoices). The main client narrative **cannot be demonstrated** until `../demo/CLIENT_DEMO_OPERATOR_RUNBOOK.md` Parts 2–11 (or equivalent) are completed in this environment.

---

## Genuine Demo Blockers

1. **Demo Care Group does not exist** — Platform API lists a single tenant: **Existing Organisation** (Id=1, **Active**). No **Demo Care Group**.
2. **TenantAdmin account missing** — `demo-admin@example.com` is not in `AspNetUsers`; API log: *Login failed … unknown or inactive user*.
3. **Existing Organisation is still active** — Must be **inactive** before demo to reduce tenant-selection confusion; currently **Active=true**.
4. **No demo residents or billing data** — `Clients` count **0**; no **Alex Morgan**, **Jordan Blake**, contracts, funders, **INV-0001**, **INV-0002**, or **CN-0001** in `CareHomeDb`.
5. **Main demo path requires TenantAdmin** — PlatformAdmin (`admin@localhost`) cannot access tenant routes (`/api/clients`, `/api/invoices` return **403**); presenter cannot walk invoices, PDF, payment, credit note, or reports without completing org setup first.

---

## Demo Warnings

| Area | Warning |
|------|---------|
| **Credit note £600 narrative** | Per `../archive/CREDIT_NOTE_INVESTIGATION.md`, the UI does **not** send `lineAmounts`. Preview/generate without overrides credits **full remaining** on matched lines (~**£2,657.14** for INV-0002), not **£600** from a 7-day period. **Do not** claim £600 unless Preview shows £600. |
| **Outstanding report vs credits** | Outstanding report uses invoice **header** totals; does not net credit notes. |
| **Email UX** | Development simulation is correct (`Email:Mode=Development`); toast may still say *queued/sent successfully* — disclose simulation verbally. |
| **Sequential billing** | Alex and Jordan must not both exist when generating INV-0001. |
| **Reports date filters** | Invoices-by-client needs **Aug 2026** date range. |
| **Client profile tab** | Label is **Funding contracts**, not “Funding”. |
| **Existing Organisation** | Remains visible to PlatformAdmin even when inactive — prepare one-line explanation. |
| **Billing disclaimer** | Proration rules are **demo only — not finance approved**. |
| **Credentials in repo** | `README.md` / `appsettings.Development.json` contain dev passwords — keep off screen. |
| **Fresh stack timing** | Containers were recently started; Angular inside `carehome-web` may need 30–90s after `up` before login is reliable. |

---

## Presenter Notes

Explain **before or during** the session (as applicable):

1. **Today’s environment gap:** If blockers above are not cleared, state that rehearsal data must be loaded per the operator runbook — do not improvise live data entry in front of the client unless that is the agreed plan.
2. **Email:** In Development, **no real email is sent**; success means **simulated delivery** (API logs `EMAIL SIMULATED`). Do not present simulation as inbox delivery.
3. **Credit notes:** Credit **period** filters which invoice **lines** are eligible; it does **not** prorate amount by days. Partial amounts require API `lineAmounts` (not in current UI). Rehearse **Preview** and align narrative to the **Credit** column — likely **£2,657.14** if generating against full August line on INV-0002.
4. **Outstanding report:** After a credit note, Jordan’s invoice row may still show **£2,657.14**; credits are separate documents.
5. **Payment status:** **Paid** / **Not paid** is a flag, not a payment ledger or partial allocation.
6. **Navigation:** Residents appear under **Clients**; funding is under **Funding contracts**.
7. **August totals vs weekly rate:** 31-day month proration exceeds “4 × weekly rate” — use finance sign-off disclaimer.
8. **Existing Organisation:** Migration placeholder tenant; inactive for demo, may still appear in platform list.
9. **Wrong login:** PlatformAdmin sees only **Organisations** — use **TenantAdmin** for the main demo.
10. **Bank details / PII:** Fictional demo data only.

---

## Final Pre-Demo Checklist

Complete **immediately before** the client joins (after runbook data entry is finished).

### Infrastructure (verified 16 Sep 2026 — re-check same day)

- [x] `docker compose ps` — `carehome-sql`, `carehome-api`, `carehome-web` up; SQL + API **healthy**
- [x] `GET http://localhost:5092/health/live` and `/health/ready` → **Healthy**
- [x] `GET http://localhost:4200/health/live` and `/health/ready` via proxy → **Healthy**
- [x] `http://localhost:4200/login` → **200**, Care Home sign-in
- [x] SQL Server host port **14333** (container healthy)
- [ ] Re-run health checks if containers were restarted

### Authentication and tenancy (**not met on 16 Sep live check**)

- [ ] **Demo Care Group** exists and is **Active**
- [ ] **Existing Organisation** is **Inactive**
- [ ] `demo-admin@example.com` exists; login succeeds with rehearsed password
- [ ] Forced password change **completed**; lands on `/dashboard` with **Demo Care Group** in header
- [ ] Clean browser session (`localStorage` key `carehome.auth` or fresh profile)

### Core demo data (**not met on 16 Sep live check**)

- [ ] Alex Morgan (`DEMO001` / `RVH-001`) and Jordan Blake (`DEMO002` / `RVH-002`)
- [ ] Funding contracts (Anytown Council, General Care, rates/dates per guide)
- [ ] **INV-0001** — **£2,546.43** — **Paid**
- [ ] **INV-0002** — **£2,657.14** — **Not paid**
- [ ] **CN-0001** — amount matches **Preview** (see credit-note warning; £600 only if Preview shows £600)
- [ ] INV-0002 PDF downloads as valid PDF

### Workflow spot-checks (after data exists)

- [ ] Dashboard, organisation settings, client profiles, billing workspace
- [ ] Simulated email on INV-0002 with verbal disclaimer
- [ ] Outstanding report — INV-0002 only
- [ ] Invoices by client — Aug 2026 — correct amounts
- [ ] `/audit` shows setup actions
- [ ] Optional: `demo-viewer@example.com` ReadOnly segment

### Presenter environment

- [ ] No IDE, README, terminal, or org-create temp password on screen
- [ ] Platform admin password only in password manager

---

## Live verification detail (16 September 2026)

### Infrastructure

| Check | Result | Notes |
|-------|--------|-------|
| Docker containers | **PASS** | `carehome-sql`, `carehome-api`, `carehome-web` running; SQL + API **healthy**; web **healthy** |
| API reachable | **PASS** | `http://localhost:5092` |
| Frontend reachable | **PASS** | `http://localhost:4200/login` HTTP 200 |
| SQL Server reachable | **PASS** | Container healthcheck passing; host **14333** |
| `/health/live` (API + proxy) | **PASS** | `status: Healthy` |
| `/health/ready` (API + proxy) | **PASS** | `status: Healthy` |

### Demo tenant

| Check | Result | Notes |
|-------|--------|-------|
| Demo Care Group exists | **BLOCKER** | Not in `Tenants` table |
| Existing Organisation inactive | **BLOCKER** | Id=1, **IsActive=1** |
| TenantAdmin exists | **BLOCKER** | Only `admin@localhost` (PlatformAdmin) in `AspNetUsers` |
| TenantAdmin login | **BLOCKER** | `demo-admin@example.com` → 401, unknown/inactive user |
| Password change complete | **BLOCKER** | No tenant admin account |
| Correct tenant selected | **BLOCKER** | N/A without Demo Care Group |

### Demo data (database + API)

| Record | Result | Notes |
|--------|--------|-------|
| Alex Morgan | **BLOCKER** | No clients |
| Jordan Blake | **BLOCKER** | No clients |
| Contracts / funders | **BLOCKER** | No clients |
| INV-0001 (£2,546.43) | **BLOCKER** | No invoices |
| INV-0002 (£2,657.14) | **BLOCKER** | No invoices |
| CN-0001 | **BLOCKER** | No credit notes; amount **not** live-tested |

**PlatformAdmin login:** **PASS** (`admin@localhost`, Development plaintext login allowed).

### Main demo walkthrough (actual environment)

| # | Step | Classification | Finding |
|---|------|----------------|---------|
| 1 | Login | **BLOCKER** | TenantAdmin missing; PlatformAdmin works but wrong role for demo |
| 2 | Dashboard | **BLOCKER** | No tenant dashboard narrative without Demo Care Group |
| 3 | Organisation | **BLOCKER** | Tenant settings unavailable to presenter login |
| 4 | Resident | **BLOCKER** | No residents |
| 5 | Funding/contract | **BLOCKER** | No data |
| 6 | Invoice | **BLOCKER** | No invoices |
| 7 | PDF | **BLOCKER** | No invoice to download |
| 8 | Payment/status | **BLOCKER** | No INV-0001/0002 |
| 9 | Credit note | **WARNING** | UI/API behaviour documented in `../archive/CREDIT_NOTE_INVESTIGATION.md`; live Preview **not run** (no INV-0002). £600 scenario **not achievable** via UI without Preview showing £600 |
| 10 | Email simulation | **PASS** | `Email:Mode=Development` in `appsettings.Development.json`; API in **Development**; simulated sends only |
| 11 | Reports | **BLOCKER** | No invoice data to report |
| 12 | Audit | **WARNING** | Route exists; minimal audit story without tenant setup |
| 13 | Optional ReadOnly | **BLOCKER** | `demo-viewer@example.com` not present |

### Credit note (live + investigation)

| Item | Status |
|------|--------|
| UI payload | Documented: `clientId`, `periodStart`, `periodEnd`, `reason`, `creditNoteDate` — **no `lineAmounts`** |
| Live Preview amount | **Not executed** — no Jordan / INV-0002 in database |
| Generated CN-0001 in DB | **None** |
| £600 scenario achievable in UI? | **No** (per `../archive/CREDIT_NOTE_INVESTIGATION.md`) unless Preview shows £600; expect **£2,657.14** full-line credit when data exists |
| API force £600 | **Not attempted** (per instructions) |

### Client experience (with current empty tenant)

| Risk | Severity |
|------|----------|
| Empty dashboard / lists | **BLOCKER** if client joins before data entry |
| Wrong tenant (Existing Organisation active) | **BLOCKER** if user lands in wrong org after setup |
| Credit note amount vs script | **WARNING** when data exists — narrative mismatch |
| Email toast vs simulation | **WARNING** |
| PlatformAdmin on tenant URLs | **WARNING** — 403 / forbidden |

### Email

| Check | Result |
|-------|--------|
| Development simulation active | **PASS** — `Email:Mode=Development` |
| Real SMTP delivery | **PASS** — not configured for this mode |
| Classify simulated send as real delivery | **N/A** — no invoice email exercised live |

---

## Financial consistency (documented targets — not live-confirmed)

| Document | Expected amount | Live DB (16 Sep) |
|----------|-----------------|------------------|
| INV-0001 (Alex Morgan) | **£2,546.43** | Not present |
| INV-0002 (Jordan Blake) | **£2,657.14** | Not present |
| CN-0001 (demo guide) | **£600.00** narrative | Not present; UI likely **£2,657.14** if generated per investigation |

Formulas remain consistent with documentation and prior `docs/SYSTEM_VERIFICATION_REPORT.md`; **amounts in this Docker volume are unverified** because invoices do not exist.

---

## Prior desk review (15 September 2026)

An earlier read-only review found the stack **not running** and could not live-verify data. **16 September live check:** stack is **running**, but data preparation is **still incomplete** (equivalent to fresh `docker compose up` after migrate).

---

## Inspection methods (final live pass)

| Method | Outcome |
|--------|---------|
| `docker ps` / health HTTP | All services healthy |
| API login (PlatformAdmin) | Success |
| `GET /api/platform/tenants` | 1 tenant: Existing Organisation (active) |
| `CareHomeDb` SQL read (documented Compose SA default) | 0 clients, 0 invoices, 0 credit notes |
| API login `demo-admin@example.com` | 401 unknown/inactive |
| Config review (`appsettings.Development.json`, `../archive/CREDIT_NOTE_INVESTIGATION.md`) | Email simulation + credit UI gap confirmed |
| UI/browser automation | Not used; login page HTTP check only |
| Password guessing / API mutation | Not performed |

---

*Final live verification only. No application code, database data, configuration, or infrastructure was modified.*
