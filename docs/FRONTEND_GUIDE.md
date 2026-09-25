# Frontend guide

Angular 22 standalone app in `frontend/care-home-web`.

- Proxy: `proxy.conf.json` → `http://localhost:5092`
- Dates: `yyyy-MM-dd` strings in forms and API payloads. Use `app-date-field` (Material calendar picker) in the UI; do not wrap values in JavaScript `Date` objects in request models.
- API errors: `src/app/core/api-error.ts`
- Auth: `AuthService` (signal + localStorage), `authInterceptor`, `authGuard` / `adminGuard`

## Navigation

Dashboard · Operations (Companies, Care Homes, Clients) · Billing Setup · Billing Workspace / Invoices / Credit Notes / Misc · Reports · Sage50 · Administration (Users, Audit, Organisation) · Platform (Organisations, PlatformAdmin only)

Product title: **Care Home Back Office**. Shell subtitle: organisation name. Client means resident.

## Client profile

`/clients/:id` keeps details, funding contracts, rate history, and invoices together. `/clients/:id/edit` remains the details form.

## Important screens

| Route | Component |
|---|---|
| `/login` | `features/login/login.ts` |
| `/dashboard` | `features/dashboard/dashboard.ts` |
| `/billing` | `features/billing/pages/billing-workspace` |
| `/invoices/:id` | `features/invoices/pages/invoice-detail` |
| `/clients/:id` | `features/clients/pages/client-profile` |

PDF download uses `HttpClient` blob requests so the JWT is sent. `window.open('/api/...')` would not attach the token.
