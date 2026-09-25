# Form field requirements (A11 audit)

**Phase:** A2.5  
**Date:** 2026-09-23  
**Convention (UI):** `*` = required; `mat-hint` “Optional” on non-required fields where helpful; system-generated fields are read-only with “Generated automatically” hint where shown.

| Form | Field | Required? | User editable? | Generated? | Source of truth |
|------|-------|-----------|----------------|------------|-----------------|
| Company | Name | Yes | Yes | No | `CreateCompanyRequest.Name` [Required]; `Company.Name` |
| Company | Is active | No (create) / Yes (update payload) | Yes (edit only) | No | `UpdateCompanyRequest.IsActive`; deactivate rules in `CompaniesController` |
| Company | Id / PublicId | — | No | Yes | DB; not on create form |
| Care home | Company | Yes | Yes (create) | No | `CreateCareHomeRequest.CompanyId` [Range(1,…)] |
| Care home | Code | Yes | Yes | No | `CreateCareHomeRequest.Code` [Required]; unique per company |
| Care home | Name | Yes | Yes | No | `CreateCareHomeRequest.Name` [Required] |
| Care home | Bed capacity | Yes (0 allowed) | Yes | No | `CreateCareHomeRequest.BedCapacity` [Range(0,…)] |
| Care home | Address, phone, email | Optional | Yes | No | Optional on DTO |
| Care home | Manager name/phone/email | Optional | Yes | No | Optional on DTO |
| Care home | Is active | No (create) | Yes (edit) | No | `UpdateCareHomeRequest` |
| Care home | PublicId | — | No | Yes | Entity `CareHomeLocation.PublicId` |
| Resident | Care home | Yes | Yes (create) | No | `CreateClientRequest.CareHomeId` [Range(1,…)] |
| Resident | First / last name | Yes | Yes | No | [Required] on DTO |
| Resident | Care type | Yes | Yes | No | [Required] on DTO |
| Resident | Admission date | Yes | Yes | No | `CreateClientRequest.AdmissionDate` (defaults in UI) |
| Resident | Reference number | Optional on create | Yes | Auto if blank | `ClientIdentifiers.ResolveCreateIdentifiersAsync`; required on update |
| Resident | Sage ID | Optional on create | Yes | Auto if blank | Same resolver; required on update per UI/API |
| Resident | Title, DOB, email, phone, notes | Optional | Yes | No | DTO optional |
| Resident | Status / archive | — | Yes (edit flows) | No | `UpdateClientRequest` / archive endpoints |
| Funding authority | Code, name, type | Yes | Yes | No | `CreateFundingAuthorityRequest` [Required] |
| Funding authority | Contact, address | Optional | Yes | No | Nullable on DTO |
| Funding authority | Billing frequency | Yes | Yes | No | [Required]; enum validated in controller |
| Funding authority | Billing interval days | Conditional | Yes | No | Required when frequency `CustomDays` (controller + Angular validators) |
| Funding authority | Is active | No (create) | Yes (edit) | No | `UpdateFundingAuthorityRequest` |
| Funding authority | PublicId | — | No | Yes | Entity `FundingAuthority.PublicId` |
| Funding contract | Funding authority | Yes | Yes | No | `CreateFundingContractRequest.FundingAuthorityId` |
| Funding contract | Invoice category | Yes | Yes | No | Required FK |
| Funding contract | Nominal code | Yes | Yes | No | Required FK |
| Funding contract | Contract start | Yes | Yes | No | `ContractStartDate` |
| Funding contract | Contract end | Optional | Yes | No | Nullable; overlap rules in `FundingContractService` |
| Funding contract | Invoice template | Optional | Yes | No | Nullable FK override |
| Funding contract | Status | Yes (update) | Yes | No | `UpdateFundingContractRequest.Status` |
| Rate | Effective from | Yes | Yes | No | `CreateFundingRateRequest.EffectiveFrom` |
| Rate | Effective to | Optional | Yes | No | Nullable |
| Rate | Frequency | Yes | Yes | No | Required string (validated in service) |
| Rate | Amount | Yes | Yes | No | `decimal`; validated in service |
| Rate | Notes | Optional | Yes | No | Nullable |
| Rate | Close previous open-ended | Optional | Yes | No | Default true on request |
| Nominal code | Code, name | Yes | Yes | No | `CreateNominalCodeRequest` [Required] |
| Nominal code | Description | Optional | Yes | No | Nullable |
| Nominal code | Is active | No (create) | Yes (edit) | No | Update DTO |
| Invoice category | Code, name | Yes | Yes* | No | [Required]; *code read-only when `SystemDefault` |
| Invoice category | Description | Optional | Yes | No | Nullable |
| Invoice category | Is active | No (create) | Yes (edit)* | No | *Not editable for system defaults (API blocks deactivate) |
| Invoice template | Name | Yes | Yes | No | Entity/DTO [Required] |
| Invoice template | Invoice category | Yes | Yes | No | Required FK |
| Invoice template | Funding authority, care home, company | Optional | Yes | No | Nullable FKs; validated in `EnsureRelatedEntities` |
| Invoice template | Header/footer/bank/contact/email fields | Optional | Yes | No | Nullable strings on entity |
| Invoice template | Is active | No (create default true) | Yes (edit) | No | Upsert request |
| Misc charge | CSV import | Yes (to import) | Yes | No | `MiscChargesController.Preview` expects file columns per UI |
| Misc charge | Row fields | Per CSV | N/A | No | resident reference, used date, description, amount, nominal code |
| Organisation settings | Name | Yes | Yes | No | `UpdateOrganisationSettingsRequest`; controller trims/validates |
| Organisation settings | Trading name, registration, address, contact | Optional | Yes | No | Nullable on request |
| Organisation settings | Currency, timezone, prefixes, payment terms | Yes | Yes | No | Defaults on tenant settings entity |
| Organisation settings | Email from / primary colour | Optional | Yes | No | Nullable |
| Organisation settings | TenantId | — | No | Yes | Server tenant context |
| Credit note | Period start / end | Yes | Yes | No | `CreditNotePreviewRequest`; service validates range |
| Credit note | Reason | Yes | Yes | No | `CreditNoteService` — empty reason blocks preview |
| Credit note | Resident | Optional | Yes | No | Nullable `ClientId` |
| Credit note | Credit note number | — | No | Yes | Assigned on generate via document sequence |
| Credit note | Line amounts | Conditional | Yes (preview grid) | No | Cannot exceed remaining per line (service) |

---

## Create / edit flows (V1)

| Form | After successful create | After successful edit |
|------|-------------------------|------------------------|
| Company, care home, resident, funding authority, nominal, category, template | Redirect to list | Redirect to list |
| Funding contract | Return to resident funding tab | N/A (create-only route in V1) |
| Rate | Return to resident funding tab | Same |
| Organisation settings | Stay on page (save feedback) | Same |
| Credit note | Clear create fields; toast; refresh list | N/A |

---

## Notes

- Internal numeric IDs are not exposed as editable fields on V1 forms.
- Financial calculation rules (billing, credit limits) remain server-side only.
- Commercial Revenue forms are out of scope (feature disabled).
