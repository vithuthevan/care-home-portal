# Care home form — field requirements

Evidence: `CreateCareHomeRequest`, `UpdateCareHomeRequest`, `CareHomeDto`, `care-home-form` validators.

Legend: **R** = required, **O** = optional, **RO** = read-only in UI (edit-only control).

| Field | UI label | Class | Backend | Frontend validation |
|-------|----------|-------|---------|---------------------|
| companyId | Company * | R | Range(1..), active company rules on update | required, min(1) |
| code | Care home code/reference * | R | Required, Max(30), unique per tenant | required, max 30 |
| name | Care home name * | R | Required, Max(150) | required, max 150 |
| bedCapacity | Registered bed capacity * | R | Range(0..) | required, min(0) |
| address | Address | O | Max(200) | max 200 |
| phone | Phone | O | Max(30) | max 30 |
| email | Email | O | Email, Max(150) | email, max 150 |
| managerName | Manager name | O | Max(150) | max 150 |
| managerPhone | Manager phone | O | Max(30) | max 30 |
| managerEmail | Manager email | O | Email, Max(150) | email, max 150 |
| isActive | Status (Active) | O | Update only; deactivation rules in API | Edit only checkbox |

**System-generated (not entered):** `Id`, `PublicId`, `CompanyName`, `LogoPath`, `PortalAccentTheme`.

**Business reference:** `code` is the human reference (e.g. CH-001), not UUID. Do not display `PublicId` in normal UI.
