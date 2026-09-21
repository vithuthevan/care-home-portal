# QuestPDF licensing (commercial deployment)

The API uses [QuestPDF](https://www.questpdf.com/) **2024.12.3** for invoice PDFs, credit note PDFs, and some operational reports.

## License types

| Mode | When to use | Configuration |
|------|-------------|---------------|
| **Community** | Qualifies under QuestPDF Community license (revenue/team limits per QuestPDF terms) | `QuestPdf:LicenseType` = `Community` (default) |
| **Professional** | Commercial care-provider deployments that exceed Community eligibility | `QuestPdf:LicenseType` = `Professional` + valid QuestPDF Professional license |

Configuration is applied once at startup via `QuestPdfLicenseConfigurator` (`Documents/QuestPdfLicenseConfigurator.cs`).

## Environment variables

```text
QuestPdf__LicenseType=Professional
```

## Compliance checklist before production

1. Confirm organisation revenue and distribution model against [QuestPDF license FAQ](https://www.questpdf.com/license.html).
2. Purchase **Professional** license if Community terms do not apply.
3. Set `QuestPdf:LicenseType` in App Service configuration or Key Vault–backed settings.
4. Record license purchase and approver in internal compliance records (not in this repo).
5. Smoke-test PDF generation on the **target host OS** (Linux App Service needs fonts; see `docs/AZURE_HOSTING.md`).

## What we do not log

PDF generation logs must not include resident names, bank details, or full invoice line content at Information level.

## Changing PDF technology

Stay on QuestPDF unless licensing or a hard functional gap requires change. Any replacement must preserve tenant-scoped storage paths and authorization on download endpoints.
