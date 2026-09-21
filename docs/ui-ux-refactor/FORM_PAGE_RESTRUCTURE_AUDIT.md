# Form page restructure audit

Decision rule: **dedicated `/entity/new` page** when form has >6 fields, multiple sections, or blocks list scanning; **inline** only for quick single-row admin actions.

| Entity | Current pattern | Recommendation | Rationale |
|--------|-----------------|----------------|-----------|
| Company | Dedicated `/companies/new` | **Keep** | Simple but already separated; list is clean |
| Care home | Dedicated `/care-homes/new` | **Keep** | Many fields + company select |
| Resident | Dedicated `/clients/new` | **Keep** | Large form, identification section |
| Funding authority | Dedicated route | **Keep** | |
| Invoice category | Dedicated route | **Keep** | |
| Nominal code | Dedicated route | **Keep** | |
| Invoice template | Dedicated route | **Keep** | Complex lines |
| User | Dedicated `/users/new` | **Keep** | |
| Platform tenant | Dedicated | **Keep** | Platform only |
| Funding contract | **Embedded** in `client-profile` | **Keep inline** | Strong workflow coupling to resident |
| Funding rates | **Embedded** in client profile | **Keep inline** | |
| Misc charge import | Workspace page | **Keep** | Batch workflow |
| Credit note | Workspace with query context | **Keep** | |
| Billing generation | `billing-workspace` | **Keep** | Multi-step workflow |
| Organisation settings | Single settings page | **Keep** | Not a create form |

## Lists with no inline create (good)

Companies, care homes, residents, users, funding authorities, invoice categories, nominal codes, invoice templates — all use **Add** → navigate to `/new`.

## Post-create navigation

| Entity | Current | Target |
|--------|---------|--------|
| Company | Detail page | **List** `/companies` |
| Resident | Profile | **List** `/clients` (or filtered by home) |
| Care home | (verify form) | List `/care-homes` |
| Setup entities | Mostly list | List |

## No action

No large create forms were found embedded under list tables in the current codebase (prior refactor already moved them).
