# UI/UX refactor — QA checklist

Use after applying migration `20260921100000_AddEntityPublicIdsAndPortalTheme` and deploying API + web.

## Global / shell

| # | Check | Pass |
|---|--------|------|
| 1 | App loads after login | |
| 2 | No accent colour picker in top toolbar | |
| 3 | No accent picker in user menu | |
| 4 | Organisation settings has **no** browser accent section | |
| 5 | No horizontal scroll on nav at 1280 / 1024 / 768 | |
| 6 | Sidebar collapse still works on desktop | |

## Care home portal theme

| # | Check | Pass |
|---|--------|------|
| 25 | `/care-homes/:uuid/settings` loads Appearance section | |
| 26 | Saving theme persists (reload settings page) | |
| 27 | Theme applies on dashboard + settings only | |
| 28 | Leaving care home routes resets default green accent | |
| 29 | Platform admin / org settings do not offer portal theme | |

## UUID routes (core entities)

| # | Check | Pass |
|---|--------|------|
| 18 | Company list links use UUID when `publicId` present | |
| 19 | Company detail/edit URLs use UUID | |
| 20 | Resident list/detail URLs use UUID | |
| 21 | Invoice list/detail URLs use UUID | |
| 22 | Legacy numeric URLs still resolve (API dual key) | |

## Forms

| # | Check | Pass |
|---|--------|------|
| 14 | Resident create shows optional auto-generated reference hints | |
| 15 | Create company → redirects to `/companies` | |
| 16 | Create resident → redirects to `/clients` | |
| 17 | Empty reference/Sage on create triggers server generation | |

## Invoice payment

| # | Check | Pass |
|---|--------|------|
| 27 | Single payment status panel on invoice detail | |
| 28 | Mark paid / unpaid updates only after API success | |
| 29 | No duplicate payment badge in invoice hero | |

## Lists / tables

| # | Check | Pass |
|---|--------|------|
| 7 | Tables use full width in `table-shell` | |
| 8 | Pagination on companies, homes, residents, invoices, users, audit | |
| 9 | Setup lists (categories, nominal, templates) documented as unpaged API | |

## Regression (business logic)

| # | Check | Pass |
|---|--------|------|
| 30 | Billing preview/generate unchanged | |
| 31 | Invoice PDF download | |
| 32 | Credit notes workspace | |
| 33 | Sage export | |
| 34 | Tenant isolation / care home scoping | |

## Database

| # | Check | Pass |
|---|--------|------|
| — | `dotnet ef database update` applied | |
| — | `PublicId` populated on existing rows | |
