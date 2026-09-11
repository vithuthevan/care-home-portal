# Architect interview answers (CareHomeSystem)

Answers grounded in this codebase. Use for self-study; ask a peer or Cursor to grade.

---

### 1. Why is the isolation boundary Tenant rather than Company?

A **Tenant** is the paying organisation (SaaS customer). A **Company** is a legal entity under that organisation. Users, sequences, unique codes, and document folders are scoped per organisation. Isolating only by Company would let one org’s users see another company’s data if they shared a login plane, and would complicate platform onboarding. See `docs/MULTI_TENANCY.md`.

### 2. Why avoid EF global query filters for multi-tenancy?

PlatformAdmin has no tenant; design-time migrations and services that take an explicit `tenantId` would need `IgnoreQueryFilters()` everywhere. The team chose explicit `ForTenant` / `Id + TenantId` so platform and financial code paths stay obvious. Cost: a forgotten filter is a leak.

### 3. What claims are in the JWT and why is `tenant_id` omitted for PlatformAdmin?

Claims include `sub`, roles, optional `tenant_id`, `tenant_public_id`, `tenant_name`, and `must_change_password`. PlatformAdmin omits `tenant_id` so operational APIs with `[RequireTenant]` return 403 instead of accidentally returning unfiltered data.

### 4. How does LocationManager scoping return 404 instead of 403, and why?

`UserAccessService` filters lists and `CanAccessCareHomeAsync` gates by-id reads. Out-of-scope same-tenant resources return **404** so the API does not confirm that an unassigned home/client/invoice exists (enumeration / existence leak).

### 5. Walk through billing eligibility from occupancy to unbilled fragments.

Non-archived client → occupancy ∩ requested period → Active contracts overlapping that window → reject overlapping Active contracts on same Authority+Category → rate periods overlapping → subtract non-void invoice line service periods for same client+contract+category → remaining date fragments become billable lines (plus eligible misc charges).

### 6. Why does the invoice header have no `ClientId`?

Invoices are grouped by Company + Care Home + Funding Authority + Invoice Category and may cover **many** clients. Clients live on `InvoiceLine` with per-line snapshots.

### 7. What is an invoice snapshot and why must PDFs not re-read live master data?

At generate time, names/codes/rates/bank/contact text are copied onto invoice/line columns. PDFs and Sage export use those snapshots so later renames or rate edits cannot rewrite financial history. Cached `PdfPath` reuses the first rendered file.

### 8. How are invoice numbers allocated under concurrency?

`DocumentSequenceService.NextAsync` selects the per-tenant sequence row with `UPDLOCK, ROWLOCK, HOLDLOCK`, increments `NextValue`, and formats `Prefix + padded number`. Used inside billing/credit transactions. Unique `(TenantId, InvoiceNumber)` backs it.

### 9. What does `sp_getapplock` protect, and what does it not protect?

Protects concurrent **billing generate** for the same tenant (`billing-generate-{tenantId}`). Does **not** serialize void, payment status, email send, credit notes, misc import, Sage export, or funding-contract writes.

### 10. How does catch-up billing avoid double-charging already-finalized days?

Preview/generate compute remaining = requested ∩ occupancy ∩ contract ∩ rate **minus** already-billed non-void service periods. Generate re-checks `HasFinalizedOverlapAsync` before insert and rolls back on conflict.

### 11. Where are rate formulas defined, and what is provisional vs locked?

`Billing/RateCalculator.cs`. Daily/weekly/monthly proration and inclusive day counting are **provisional** pending business sign-off (`docs/OPEN_BUSINESS_DECISIONS.md`, `docs/BILLING_ENGINE.md`). Snapshotting and no double-bill are treated as locked product rules.

### 12. How do credit notes cap remaining balance, and what race remains?

`RemainingCreditable` = line amount minus non-void credit line absolutes. Preview enforces requested ≤ remaining. Race: preview outside TX + no applock → two generates can both use the full remaining (over-credit). Priority 0 fix: lock + re-validate inside TX.

### 13. Why Restrict delete on financial FKs instead of cascade?

Deleting a company/client must not silently wipe invoices or credit history. Operators deactivate/archive/void instead. `OnDelete(Restrict)` forces explicit handling.

### 14. How would you detect a missed `TenantId` filter in code review?

Checklist: every operational query uses `ForTenant` or `x.TenantId == tenantId`; no `FindAsync(id)` alone for tenant data; child tables loaded only via tenant-scoped parents. Prefer automated tests that seed two tenants and assert cross-tenant GET by id returns 404.

### 15. Compare soft-deactivate (`IsActive`) vs void vs archive.

- `IsActive=false`: master data / tenant / user — retained, hidden from normal ops; inactive tenant blocks login.
- Invoice `Status=Void`: financial cancellation; may free misc charges for re-invoice.
- Client `IsArchived`: resident removed from billing eligibility without hard delete.

### 16. What happens when SMTP fails after invoice generation?

Invoice remains Generated; send endpoint returns error; `EmailSendLog` records failure. Money state is not rolled back (correct).

### 17. Why is email simulation dangerous if left on in Production?

`ConfigurableEmailSender` returns Success with Simulated=true and only logs. Operators believe emails were delivered. Production startup warns; Smtp must be configured for real delivery.

### 18. How does document storage prevent path traversal across tenants?

Paths under `tenants/{publicId}/…`; `Path.GetFileName` sanitization; reject `..`. Downloads go through authorized API that loads the entity with TenantId first.

### 19. What is the ReadOnlyGuardFilter and why is UI hiding insufficient?

Global filter returns 403 on mutating HTTP methods for pure ReadOnly roles. Angular `canWrite()` only hides buttons—attackers can still call APIs.

### 20. How does MustChangePassword interact with API middleware and Angular guards?

Claim + DB flag; `MustChangePasswordMiddleware` blocks APIs except change-password; Angular `passwordChangeGuard` / `authGuard` force `/change-password`. After change, SecurityStamp updates (and with Priority 0, old JWTs fail validation).

### 21. Design an idempotency approach for `/api/billing/generate`.

Client sends `Idempotency-Key`. Server stores `(TenantId, Key)` → response hash/body for 24h. Same key+same body replay returns stored result; same key+different body → 409. Combine with existing applock so concurrent first attempts still serialize.

### 22. How would you make Sage export crash-safe (file vs DB)?

Prefer: open TX → lock eligible invoices → insert batch + mark invoices → commit → write file → update path if needed. Or write to temp path, commit DB, then rename. Never leave “file without batch” or “batch without marks” as lasting states.

### 23. When would you introduce a background worker?

When p95 latency or timeouts hurt: bulk email, large billing runs, heavy Excel/PDF reports. Keep the same transactional core; HTTP becomes “enqueue job + poll status.”

### 24. What breaks first at 100 concurrent operators in one tenant during month-end billing?

Tenant-wide billing applock → most generates wait/fail (30s). SQL CPU on eligibility queries. PDF/SMTP if everyone downloads/sends. Credit/Sage races surface without locks.

### 25. Why store JWT in localStorage, and what is the threat model?

SPA convenience without cookie CSRF. Threat: XSS steals token. Mitigations: CSP (`SecurityHeadersMiddleware`), no inline secrets, short lifetime, stamp validation, avoid dangerous HTML sinks.

### 26. How do you rotate `Jwt:Key` without locking everyone out?

Dual-key validation briefly (accept old and new signing keys), deploy new issuer key, wait for max token lifetime, then drop old key. Documented ops step; not automated in-repo today.

### 27. What automated tests are missing for financial correctness?

Concurrency tests for credit over-credit, funding overlap TOCTOU, billing double-generate; integration tests for cross-tenant isolation; golden tests for RateCalculator once business signs off. Today: thin unit tests + heavy PowerShell UAT.

### 28. How should overlapping Active funding contracts be enforced in the database if possible?

SQL cannot easily UNIQUE arbitrary inclusive ranges. Options: exclusion constraints (PostgreSQL), serialized TX + UPDLOCK on the stream key, or applock per `(tenant, client, authority, category)` plus app check (chosen Priority 0 approach on SQL Server).

### 29. Explain same-origin SPA+API hosting vs separate frontend CDN.

Same-origin (API `wwwroot`): simpler CORS, one deploy, App Service serves both. Separate CDN: better static caching/global edge; needs CORS, dual pipelines, cookie domain care. Pilot uses same-origin (`docs/AZURE_HOSTING.md`).

### 30. If a customer demands SOC2 evidence, what observability gaps would auditors notice?

No centralized SIEM/App Insights metrics/alerts in-repo; limited auth-failure analytics; no formal access reviews export beyond AuditLog; document store backups separate from SQL; email simulation risk; dependency on manual runbooks rather than continuous monitoring.
