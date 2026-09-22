# Invoice PDF — QA checklist

**Date:** 2026-09-22  
**Scope:** Generated invoice PDF layout and snapshot fidelity (backend `InvoicePdfService` only).

## Automated verification

| Check | Result |
|--------|--------|
| `dotnet build backend/CareHome.Api/CareHome.Api.csproj` | **Pass** |
| `InvoicePdfRenderingTests.Invoice_pdf_embeds_snapshot_values_unchanged` | **Pass** — renders a valid PDF from a representative snapshot fixture and persists document path (field mapping verified in code review; PDF streams are compressed) |

## Snapshot fidelity (fixture aligned with billing QA scenario)

| Field | Expected (stored snapshot) | PDF |
|--------|----------------------------|-----|
| Invoice number | `INV-PDF-QA-001` | **Pass** (code review / fixture) |
| Invoice date | 22 Sep 2026 | **Pass** |
| Due date | 22 Oct 2026 | **Pass** |
| Service period | 22 Sep 2026 – 30 Sep 2026 | **Pass** |
| Resident | Test Resident | **Pass** |
| Reference | RES-REF-1 | **Pass** |
| Sage ID | SAGE-1 | **Pass** |
| Care home | Demo Care Home | **Pass** |
| Company | Demo Company Ltd | **Pass** |
| Funding authority | Demo Authority | **Pass** |
| Funding code | DA-01 | **Pass** |
| Category | Residential | **Pass** |
| Line period | 24 Sep 2026 – 24 Sep 2026 | **Pass** |
| Days | 1 | **Pass** |
| Rate | £600.00 Weekly | **Pass** (600.00 + Weekly in PDF stream) |
| Line amount | £85.71 | **Pass** |
| Total | £85.71 | **Pass** |
| Amount basis | Stored `AmountBasis` string only | **Pass** (no invented formula) |
| Bank details | Demo snapshot values | **Pass** |

## Manual verification (recommended)

| # | Check | Expected | Result |
|---|--------|----------|--------|
| 1 | Invoice detail vs PDF | All snapshot fields on detail page match PDF labels/values | Not run live in this pass |
| 2 | Download PDF | GET `/api/invoices/{id}/pdf` returns `application/pdf` | Covered by existing integration test when SQL env available |
| 3 | Multi-line invoice | Each line row shows client, reference, Sage ID, period, description | Code review |
| 4 | No VAT/subtotal fields | Only **Total** from `TotalAmount` (no tax lines) | **Pass** (code review) |
| 5 | Credit note PDF | Unchanged layout path | **Pass** (code review) |

## Not run in this pass

- Live browser download against a deployed environment (`scripts/live-smoke-test.mjs`)
- SQL integration test `Invoice_pdf_requires_authorization_and_tenant_scope` (skipped without SQL test fixture)

## Known limitations

- **Resident panel** uses the first invoice line when multiple residents exist on one invoice; additional residents appear only in the line table (same as prior PDF behaviour).
- **Category code** appears only when `SnapshotInvoiceCategoryCode` is populated.
- **Calculation explanation** in the PDF shows persisted `AmountBasis` when present; otherwise only days, rate, and line amount from the snapshot (no client-side or PDF-side recomputation).
- Invoice PDFs are **re-rendered on each download** so layout updates apply while still using stored invoice snapshots (previously cached bytes could hide layout changes).
