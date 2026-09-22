# Companies module — manual QA

**Date:** 2026-09-22  
**Environment:** Local or deployed portal with write access.

## Checklist

| # | Step | Expected |
|---|------|----------|
| 1 | Open **Companies** (`/companies`). | List loads without error. |
| 2 | Verify page header. | Title **Companies**; subtitle *Manage the organisations that operate your care homes.* |
| 3 | Verify only one **Add company** CTA when the list has rows. | Single primary button in page header (not duplicated in toolbar). |
| 4 | Search for a company. | Debounced/server search; pagination resets to page 1. |
| 5 | Clear search. | **Clear search** on empty-results state restores full list. |
| 6 | Change page size. | Table repaginates; total count unchanged. |
| 7 | Navigate pages. | Next/previous and page indicator work with search applied. |
| 8 | Open company (name or **View**). | Company profile loads. |
| 9 | Verify company URL uses UUID. | Path `/companies/{uuid}` (not numeric id). |
| 10 | Open **Edit company**. | Form loads with name and status. |
| 11 | Verify edit URL uses UUID. | Path `/companies/{uuid}/edit`. |
| 12 | Save company (edit). | Toast *Company updated successfully.*; redirect to `/companies`. |
| 13 | Verify redirect to Companies list after edit. | List visible. |
| 14 | Create company (`/companies/new`). | Name required; save succeeds. |
| 15 | Verify redirect to Companies list after create. | Toast *Company created successfully.*; URL `/companies`. |
| 16 | Open company care homes (**Care homes** or **More → Care homes**). | URL `/companies/{uuid}/care-homes` (or legacy `/care-homes?company={uuid}`). |
| 16b | From company profile, **View residents** (if count &gt; 0). | `/clients?company={uuid}` with company context. |
| 17 | Verify company context remains visible. | Subtitle includes company name; **Back to company** present. |
| 18 | Open care home (dashboard). | URL `/care-homes/{uuid}/dashboard`. |
| 19 | Verify care-home URL uses UUID. | No numeric id in path. |
| 20 | Test empty company state. | *No companies yet* with **Add company** (if permitted). |
| 21 | Test empty search state. | *No companies found* with **Clear search**. |
| 22 | Test company with no care homes (profile + filtered list). | *No care homes yet* copy and **Add care home** / **Back to company** on list. |
| 23 | Test keyboard navigation. | Tab through search, links, icon actions, pagination. |
| 24 | Test tooltips. | Icon buttons show tooltip matching `aria-label`. |
| 25 | Test mobile layout (~375px). | Cards visible; no horizontal page scroll. |
| 26 | Test 1280px layout. | Table uses content width; actions usable. |
| 27 | Confirm no horizontal scrolling. | Shell and tables fit viewport at 1280px and 1920px. |

## Regression notes

- Deactivate remains under **More** (write users only).
- List **Care homes** / **Residents** columns show API roll-up counts (0 when none).
- Legacy `/care-homes?companyId={int}` still filters if bookmarked.
