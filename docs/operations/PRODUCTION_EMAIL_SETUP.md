# Production email setup

**Purpose:** Configure live SMTP email delivery for invoice and credit-note sends in Production.

**Related docs:** [PRODUCTION_SECRETS_SETUP.md](PRODUCTION_SECRETS_SETUP.md), [PRODUCTION_CONFIGURATION.md](../PRODUCTION_CONFIGURATION.md), [P0_EXECUTION_ROADMAP.md](../pilot/P0_EXECUTION_ROADMAP.md)

---

## Supported provider

The application uses **SMTP** via `System.Net.Mail.SmtpClient`. Any provider that supports authenticated SMTP with TLS works, for example:

| Provider | Typical host | Port | TLS |
|----------|--------------|------|-----|
| Microsoft 365 / Exchange Online | `smtp.office365.com` | 587 | Yes |
| Google Workspace | `smtp.gmail.com` | 587 | Yes |
| SendGrid | `smtp.sendgrid.net` | 587 | Yes |
| Amazon SES | `email-smtp.<region>.amazonaws.com` | 587 | Yes |
| On-prem Exchange | `mail.<your-domain>` | 587 | Yes |

Use a dedicated **billing mailbox** (e.g. `billing@operator-domain.org`) with SMTP relay permission. Do not use personal accounts.

---

## Required configuration

### Path A — Live SMTP (recommended)

| App setting | Secret? | Required | Example |
|-------------|---------|----------|---------|
| `Email__Mode` | No | **Yes** | `Smtp` |
| `Email__FromAddress` | No | **Yes** | `billing@example.org` |
| `Email__FromName` | No | No | `Care Home Billing` |
| `Email__Smtp__Host` | No | **Yes** | `smtp.office365.com` |
| `Email__Smtp__Port` | No | No (default `587`) | `587` |
| `Email__Smtp__EnableSsl` | No | No (default `true`) | `true` |
| `Email__Smtp__User` | No | If auth required | `billing@example.org` |
| `Email__Smtp__Password` | **Yes** | If auth required | Key Vault / env var |

### Path B — Manual send (interim only)

| App setting | Value | Notes |
|-------------|-------|-------|
| `Email__AllowSimulationInProduction` | `true` | Explicit opt-in only; requires business sign-off |
| `Email__Mode` | Any value except `Smtp` | API starts; sends **fail visibly** (invoice not marked Sent) |

Path B is **not** suitable for unattended live funder invoicing. Operators must download PDFs and send via Outlook or another mail client.

---

## Azure Key Vault secrets

| App setting | Key Vault secret name |
|-------------|----------------------|
| `Email__Smtp__Password` | `Email-Smtp-Password` |

Non-secret SMTP settings are plain App Service app settings. Only the password is stored in Key Vault.

---

## Configuration steps

### Option 1 — Deploy script (Azure)

```powershell
.\scripts\Deploy-Azure.ps1 `
  -ResourceGroup rg-carehome `
  -AppName carehome-pilot `
  -EmailMode Smtp `
  -SmtpHost smtp.office365.com `
  -SmtpUser billing@example.org `
  -SmtpFromAddress billing@example.org `
  -SmtpFromName "Care Home Billing" `
  -SmtpPassword (ConvertTo-SecureString 'your-smtp-password' -AsPlainText -Force)
```

The script writes `Email-Smtp-Password` to Key Vault and sets `Email__Smtp__Password` as a Key Vault reference on App Service.

### Option 2 — Manual Azure CLI

```powershell
# Store SMTP password
az keyvault secret set --vault-name <vault> --name Email-Smtp-Password --value "<smtp-password>"

# Configure App Service
az webapp config appsettings set --resource-group <rg> --name <app> --settings `
  Email__Mode=Smtp `
  Email__FromAddress=billing@example.org `
  Email__FromName="Care Home Billing" `
  Email__Smtp__Host=smtp.office365.com `
  Email__Smtp__Port=587 `
  Email__Smtp__User=billing@example.org `
  Email__Smtp__EnableSsl=true `
  Email__Smtp__Password="@Microsoft.KeyVault(SecretUri=https://<vault>.vault.azure.net/secrets/Email-Smtp-Password/)"

az webapp restart --resource-group <rg> --name <app>
```

### Option 3 — Environment variables (IIS, systemd, Kubernetes)

```text
Email__Mode=Smtp
Email__FromAddress=billing@example.org
Email__Smtp__Host=smtp.example.org
Email__Smtp__Port=587
Email__Smtp__User=smtp-user
Email__Smtp__Password=<from host secret store>
Email__Smtp__EnableSsl=true
```

---

## Startup behaviour

| Environment | `Email__Mode` | `Email__AllowSimulationInProduction` | API startup | Send result |
|-------------|---------------|--------------------------------------|-------------|-------------|
| Development | `Development` | (ignored) | Starts | Success, simulated |
| Production | `Smtp` + valid settings | `false` | Starts | Real SMTP delivery |
| Production | not `Smtp` | `false` | **Fails** | — |
| Production | not `Smtp` | `true` | Starts (warning) | **Fails visibly** |

Production **never** silently marks emails as successfully delivered when SMTP is not configured.

---

## Failure behaviour

### API responses

| Scenario | HTTP | User message |
|----------|------|--------------|
| SMTP not configured (Production) | 400 | Email delivery is not configured… |
| SMTP auth failure | 400 | SMTP authentication failed… |
| SMTP connection failure | 400 | Could not connect to the SMTP server… |
| Recipient rejected | 400 | SMTP server rejected one or more recipients. |
| Missing recipient on invoice | 400 | This invoice has no recipient email. |

Invoice and credit-note status (`Sent`, `SentAt`) is updated **only** when `Success=true` and `Simulated=false`.

### Logging

- Successful sends: recipient, subject, attachment name (no body content).
- Failed sends: exception type, sanitized summary, sanitized detail.
- **Passwords and SMTP credentials are never logged.** `EmailLogSanitizer` redacts configured secrets and common `password=` patterns from log output.

### Database

`EmailSendLogs` records each attempt with `Success`, `Simulated`, and `ErrorMessage` for operator audit.

---

## Verification

1. **Startup log** — confirm `Email mode is Smtp. Host=... From=...` (not a simulation warning).
2. **Health** — `GET /health/ready` returns `Healthy`.
3. **Test send** — send a test invoice from staging or production to a monitored mailbox.
4. **Confirm delivery** — correct from address, PDF attached, expected subject/body.
5. **Audit** — `EmailSendLogs` shows `Success=true`, `Simulated=false`.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| API refuses to start: "Production requires Email:Mode=Smtp" | SMTP not configured | Set `Email__Mode=Smtp` and required settings, or Path B flag with sign-off |
| API refuses to start: password missing | User set without password | Add `Email-Smtp-Password` to Key Vault and reference from `Email__Smtp__Password` |
| `SMTP authentication failed` | Wrong user/password or MFA blocking basic auth | Verify mailbox SMTP auth policy; use app password or SMTP relay account per provider |
| `Could not connect to SMTP server` | Wrong host/port or firewall | Verify outbound TCP 587 from App Service; check provider docs |
| Send returns 400 but startup OK | Transient SMTP error | Check App Service logs; verify recipient address |
| Key Vault reference not resolving | Managed identity RBAC | Confirm App Service identity has **Key Vault Secrets User** on the vault |
| Invoice still Draft after send | Simulation or failure path | Check API response message and `EmailSendLogs.ErrorMessage` |

### Microsoft 365 notes

- SMTP AUTH must be enabled for the mailbox (Exchange admin center).
- Use the mailbox UPN as `Email__Smtp__User`.
- If basic auth is disabled tenant-wide, use a relay connector or transactional provider (SendGrid, SES).

---

## Security

- Store `Email__Smtp__Password` only in Key Vault or the host secret store — never in git, Bicep parameters, or deploy script output.
- Use a dedicated billing mailbox with least privilege.
- Rotate SMTP passwords per provider policy; update Key Vault and restart App Service.
- Failed-send logs contain sanitized messages only — no credentials.

---

## File reference

| File | Purpose |
|------|---------|
| `backend/CareHome.Api/Email/ConfigurableEmailSender.cs` | SMTP send and simulation handling |
| `backend/CareHome.Api/Email/EmailOptions.cs` | Configuration model |
| `backend/CareHome.Api/Email/EmailLogSanitizer.cs` | Safe logging (no secrets) |
| `backend/CareHome.Api/Security/ProductionStartupValidator.cs` | Production fail-fast email validation |
| `scripts/Deploy-Azure.ps1` | Optional `-EmailMode Smtp` deploy parameters |
| `infra/azure/main.bicep` | No longer forces `Email__Mode=Development` |
