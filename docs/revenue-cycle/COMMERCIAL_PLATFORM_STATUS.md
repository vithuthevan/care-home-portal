# Commercial platform status

| Capability | Status | Notes |
|------------|--------|-------|
| Banking & reconciliation | **COMPLETE** | Manual editor, CSV mapping templates, reversal, invoice search filters, SQL integration tests expanded |
| Remittances | **PARTIAL** | CSV/XLSX import, auto-match, confirm → payment; PDF extraction **NOT STARTED** |
| Revenue assurance | **PARTIAL** | Deterministic scan + findings UX; impact calc on subset of rules |
| Occupancy vs billing | **PARTIAL** | Active-resident-not-billed rule; dedicated monthly reconciliation UI **NOT STARTED** |
| Contract renewals | **PARTIAL** | Renewal entity, dashboard, agree → new effective-dated rate |
| Disputes | **PARTIAL** | Open/list/resolve/messages API + list UX; documents/portal **NOT STARTED** |
| Collections | **PARTIAL** | Tenant policy + AR-derived dashboard; automated reminders **NOT STARTED** |
| Funder account / statements | **NOT STARTED** | Use AR funder rollups; statement PDF **NOT STARTED** |
| Funder portal | **NOT STARTED** | Requires external auth boundary |
| Executive analytics | **PARTIAL** | `/api/finance/attention` KPIs; full analytics **NOT STARTED** |
| AI revenue assistant | **NOT STARTED** | **BLOCKED — EXTERNAL CREDENTIALS** (provider) |
| Background jobs | **NOT STARTED** | On-demand scans only |
| Accounting integrations (Xero/QBO) | **NOT STARTED** | Sage export remains |
| SaaS subscription billing | **NOT STARTED** | |
| Enterprise SSO / Entra | **NOT STARTED** | JWT remains primary |
| Demo seed profile | **PARTIAL** | Existing `DevelopmentMasterDataSeeder`; commercial demo pack **NOT STARTED** |
