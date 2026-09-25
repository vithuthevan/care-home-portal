# Commercial product readiness

## Product scope

Care-provider revenue cycle: funding → billing → AR → payments → banking → remittance → assurance → disputes/collections, single modular monolith (.NET 10 + Angular 22 + SQL Server).

## Buyer

Multi-site care groups with council/NHS/private funders and centralised finance teams.

## Deployment architecture

Single deployable `CareHome.Api` with modules: Billing, Funding, Receivables, Payments, Reconciliation, Remittance, RevenueAssurance.

## Security status

JWT + capability policies; tenant context on APIs; cross-tenant integration tests for banking/remittance. Funder portal not implemented.

## Pilot deployment requirements

- SQL Server + connection string
- JWT signing key
- SMTP for invoice email
- Apply EF migration `CommercialRevenueCycleExtensions`
- CORS origins for Angular host

## Known P0

- Remittance PDF ingestion and funder statements not available
- No production-grade job runner for scheduled assurance/collections
- Funder/private portals absent

## Known P1

- Occupancy reconciliation dashboard
- Full executive analytics SQL projections
- Write-offs not modelled
