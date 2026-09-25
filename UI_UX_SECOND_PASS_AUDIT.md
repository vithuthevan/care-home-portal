# Care Home — UI/UX second-pass audit

Second-pass goal: professional SaaS finance-operations product (not a generic Angular admin shell). Business logic, billing calculations, auth, and APIs were not changed.

## Page type map

| Page | Type | Primary job |
|------|------|-------------|
| Dashboard | DASHBOARD | Operations centre — attention, finance KPIs, recent activity |
| Companies list | LIST | Find / add operating companies |
| Company detail | DETAIL | Company context + related homes / residents / users |
| Care homes list | LIST | Find / add homes |
| Care home dashboard | DETAIL (ops) | Home-level occupancy and residents |
| Residents list | LIST | Find residents, filter by home |
| Resident profile | DETAIL / workspace | Funding, rates, billing context |
| Funding authorities, nominal codes, categories, templates | LIST / SETUP | Configure billing prerequisites |
| Billing workspace | WORKFLOW | Scope → preview → review → generate |
| Invoices list | LIST | Search, bulk pay, open invoice |
| Invoice detail | TRANSACTION DETAIL | Amount, payment, PDF, lines |
| Credit notes | WORKFLOW | Resident / invoice context |
| Reports | REPORT | Run, summarise, export |
| Audit | LIST | Compliance trail |
| Organisation settings | SETTINGS | Tenant configuration |
| Users | LIST / SETUP | Administration |

---

## Application shell

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Global | Breadcrumbs duplicated in toolbar and page title area | One trail at page level | Breadcrumbs moved into `app-page-header`; toolbar is menu + theme + user only | Clear “where am I?” without repetition | Dynamic entity names still set per-page via `BreadcrumbService.set()` |
| Sidebar | No desktop collapsed mode | Compact icon rail + tooltips | 248px expanded / 72px collapsed; persisted in `localStorage`; menu button toggles collapse on desktop | SaaS density on large screens | Collapsed mode shows all section links as icons (dense); could add “favourites” later |
| Sidebar | 272px felt wide | Tighter nav | Reduced width, icon-only nested links when collapsed | More content width | — |

---

## Shared primitives

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Page header | Title only | Breadcrumb + title + subtitle + actions | `PageHeaderComponent` reads `BreadcrumbService` | Matches finance SaaS pattern | Login/forbidden can pass `showBreadcrumbs=false` if needed |
| Detail pages | No standard financial context strip | Reusable summary strip | `app-entity-summary-strip` on resident profile and care home dashboard | Financial / ops context at a glance | Other entities (funding authority) still use KPIs only |
| Lists | Filters floating above unrelated chrome | Toolbar fused to table | `table-shell` + `app-filter-bar [toolbar]` on residents, companies, care homes, invoices | LIST page pattern from brief | Some setup lists still use standalone filter panels |
| KPI cards | Heavy padding | Compact tinted cards | Tighter `.kpi-card` padding in global styles | Dashboard / detail density | — |

---

## Dashboard

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Dashboard | Felt like KPI cards + generic panels | Operations centre | Renamed to “Operations centre”; primary CTA “Start billing”; attention queue list; finance/ops KPIs unchanged (API) | Answers “what needs attention?” | Upcoming billing still table-only (no drill-through without new API) |

---

## Companies

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Company list | Filter separate from table | LIST pattern | Search inside `table-shell`; add action in toolbar | Consistent list UX | — |
| Company detail | Dead-end overview | Tabs: Details, Care homes, Residents, Users | `mat-tab-group`; homes in `table-shell`; residents/users deep-link | Organisation → company → home → resident | No per-company user API — Users tab links to `/users` |

---

## Residents

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Resident list | Filters outside table shell | LIST pattern | Toolbar inside `table-shell`; primary add in toolbar | Matches spec layout | Funding column not added (no rate on list DTO) |
| Resident profile | Financial context buried in panels | Summary strip + tabs | Strip: funding, rate, period, outstanding (from loaded invoices) | Financial workspace | Outstanding uses client invoice list, not a dedicated balance API |

---

## Billing / invoices / payments

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Billing | Already workflow-oriented | Keep WORKFLOW type | No logic change; existing `app-workflow-steps` | — | Step panels still form-like; could add preview hero metrics only (data exists after preview) |
| Invoice list | Toolbar separate from grid | LIST pattern | Unified `table-shell` when results or active filters | Cleaner hierarchy | Empty-with-filters uses same shell (filter visible) |
| Invoice detail | Back button + duplicate PDF | TRANSACTION DETAIL | PDF primary in page header; payment status on hero; duplicate PDF removed from action row | Action hierarchy | “More” menu for secondary actions still partial (icon row remains) |

---

## Credit notes / reports / audit / settings

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Credit notes | — | Resident search, not raw IDs | No change (first pass) | — | — |
| Reports | API keys in headers | Human labels | No change (`columnLabel` map already present) | — | Full-result reports still unpaginated by design |
| Audit | Filter outside table | LIST `table-shell` | Entity filter in toolbar; event count + sticky table | Matches finance list pattern | — |
| Billing | Preview totals buried in step text | Preview hero | `billing-preview-hero` (period, scope, residents, total, exceptions) | Workflow clarity | — |
| Setup lists | Panel tables | `table-shell` + toolbar | Funding authorities, nominal codes, categories, templates, **users**, **misc charges** | Setup pages feel like product lists | — |
| Settings | — | SETTINGS | Theme picker in toolbar + user menu (existing) | Accent themes per brief | — |

---

## Identifiers

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Global | Risk of exposing DB IDs | Business references in UI | Routes still use numeric `:id`; UI shows reference numbers, invoice numbers, home codes | No migration without backend graph | Invoice detail shows `clientReference` on hero when present |

---

## Responsive & accessibility

| Page | Current problem | Design decision | Implemented change | Reason | Remaining issue |
|------|-----------------|-----------------|-------------------|--------|-----------------|
| Global | — | Page never scrolls horizontally | Existing `overflow-x: hidden`; tables in `table-container` | — | Manual QA at 1024/768 still required |
| Shell | — | Keyboard / ARIA | Collapse toggle `aria-label`; breadcrumbs `nav`; icon actions unchanged | — | Full keyboard pass documented in manual QA |

---

## Files touched (second pass)

- `frontend/care-home-web/src/app/app.html`, `app.ts`, `app.scss`
- `frontend/care-home-web/src/app/shared/ui/page-header.ts`, `entity-summary-strip.ts`
- `frontend/care-home-web/src/styles.scss`
- `frontend/care-home-web/src/app/features/dashboard/dashboard.html`
- `frontend/care-home-web/src/app/features/clients/pages/client-list/client-list.html`
- `frontend/care-home-web/src/app/features/clients/pages/client-profile/*`
- `frontend/care-home-web/src/app/features/companies/pages/company-list/company-list.html`
- `frontend/care-home-web/src/app/features/companies/pages/company-detail/*`
- `frontend/care-home-web/src/app/features/care-homes/pages/care-home-list/care-home-list.html`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-list/invoice-list.html`
- `frontend/care-home-web/src/app/features/invoices/pages/invoice-detail/invoice-detail.html`
