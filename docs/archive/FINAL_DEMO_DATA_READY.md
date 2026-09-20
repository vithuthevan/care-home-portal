# Final Demo Data Ready

**Date:** 17 September 2026  
**Session:** Local client demo data preparation completed for tenant **1004** via authenticated application API (equivalent to normal UI operations).  
**Environment:** Docker Compose Development (`http://localhost:4200`, `http://localhost:5092`).  
**TenantAdmin:** `demo-admin1@example.com` — login **succeeded** (`mustChangePassword`: false).  
**Active organisation:** **Demo Care Group** (tenant Id **1004**).  
**Constraints honoured:** No application source, schema, billing logic, credit-note logic, auth, Docker, or production changes. No `docker compose down -v`. No Identity SQL or destructive SQL.

**Notes for presenter:**

- **INV-0001** invoice line snapshot still shows **Alexa Morgan** (generated before the first-name edit). The **resident record** is **Alex Morgan** / `RVH-001`. The **Invoices by client** report uses the line snapshot, so it may list **Alexa Morgan** for INV-0001 — narrate that invoice PDFs/lines are point-in-time snapshots.
- **Credit note:** Preview only (not generated). Preview total **£2,657.14** (full remaining line per `../archive/CREDIT_NOTE_INVESTIGATION.md` — not £600).
- **ReadOnly user:** Optional step skipped — user create returned invalid role via API; create `demo-viewer@example.com` manually in **Users** if needed.

---

## Requirement matrix

| Requirement | Expected | Actual | PASS/FAIL |
|-------------|----------|--------|-----------|
| Active organisation | Demo Care Group | Demo Care Group (tenant 1004) | PASS |
| Company | Demo Care Ltd | Demo Care Ltd | PASS |
| Care home | River View House | River View House (`RIVER01`) | PASS |
| Alex | Alex Morgan / RVH-001 | Alex Morgan / RVH-001 (current resident) | PASS |
| Alex rate | £575/week | £575.00 weekly from 2026-04-01, open-ended | PASS |
| Jordan | Jordan Blake / RVH-002 | Jordan Blake / RVH-002 | PASS |
| Jordan rate | £600/week | £600.00 weekly from 2026-05-15 | PASS |
| INV-0001 | £2,546.43 | £2,546.43 | PASS |
| INV-0001 payment | Paid | Paid | PASS |
| INV-0002 | £2,657.14 | £2,657.14 | PASS |
| INV-0002 payment | NotPaid | NotPaid | PASS |
| INV-0002 PDF | Downloadable | PDF returned (%PDF, ~58 KB) | PASS |
| Email | Simulated successfully | `simulated: true` on INV-0002 send (Development mode) | PASS |
| Credit note preview | Actual preview amount recorded | **£2,657.14** (Preview only; CN not generated) | PASS |
| Outstanding report | INV-0002 | INV-0002 only (INV-0001 not outstanding) | PASS |
| Invoices by client | Both August invoices | INV-0001 £2,546.43 (Paid), INV-0002 £2,657.14 (NotPaid); INV-0001 line name snapshot **Alexa Morgan** | PASS *(amounts/periods; see snapshot note)* |
| Audit | Accessible | TenantAdmin audit lists generate, payment, send, client/contract/rate updates | PASS |
| ReadOnly | Optional | Not created (`demo-viewer@example.com`) | FAIL *(optional)* |

---

## DEMO DATA READY: **YES**

### Presenter sequence

Login → Dashboard → Alex → Funding → Start Billing → August Preview → INV-0001 → PDF → Mark Paid → Jordan → August Preview → INV-0002 → PDF → Email simulation → Credit Note Preview → Reports → Audit → Optional ReadOnly

### Billing preview confirmation (August 2026)

- **Jordan Blake:** **£2,657.14** (Preview before generate).
- **Alex Morgan:** skipped — `ALREADY_FULLY_BILLED` for August 2026.
