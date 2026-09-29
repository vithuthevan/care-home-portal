using System.Globalization;
using CareHome.Api.Dtos.Collections;
using CareHome.Api.Dtos.DataTransfer;
using CareHome.Api.Dtos.FundingContracts;
using CareHome.Api.Funding;
using CareHome.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed partial class DataTransferService
{
    private static class ExtendedCatalog
    {
        public static IEnumerable<EntityDefinition> Definitions() =>
        [
            FundingContracts(),
            FundingRates(),
            OrganisationSettings(),
            CollectionsPolicy(),
            CreditNotes(),
            MiscCharges(),
            SageExports(),
            ContractRenewals(),
            RevenueAssuranceFindings(),
            BankAccounts(),
            BankTransactions(),
            RemittanceBatches(),
            PlatformTenants(),
        ];

        public static EntityDefinition FundingContracts() => new()
        {
            Key = "funding-contracts",
            Label = "Funding contracts",
            SupportsImport = true,
            SupportsExport = true,
            Columns =
            [
                "ClientReference", "FundingAuthorityCode", "InvoiceCategoryCode", "NominalCode",
                "ContractStartDate", "ContractEndDate", "Status", "InvoiceTemplateName"
            ],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.ClientFundingContracts.AsNoTracking()
                    .Include(x => x.Client)
                    .Include(x => x.FundingAuthority)
                    .Include(x => x.InvoiceCategory)
                    .Include(x => x.NominalCode)
                    .Include(x => x.InvoiceTemplate)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Client.ReferenceNumber)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("ClientReference", x.Client.ReferenceNumber),
                    ("FundingAuthorityCode", x.FundingAuthority.Code),
                    ("InvoiceCategoryCode", x.InvoiceCategory.Code),
                    ("NominalCode", x.NominalCode.Code),
                    ("ContractStartDate", x.ContractStartDate.ToString("yyyy-MM-dd")),
                    ("ContractEndDate", x.ContractEndDate?.ToString("yyyy-MM-dd") ?? ""),
                    ("Status", x.Status),
                    ("InvoiceTemplateName", x.InvoiceTemplate?.Name ?? ""))).ToList();
            },
            PreviewImportAsync = async (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("funding-contracts", fileName);
                var clients = await svc.Db.Clients.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var authorities = await svc.Db.FundingAuthorities.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId).ToListAsync(ct);
                var categories = await svc.Db.InvoiceCategories.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId).ToListAsync(ct);
                var nominals = await svc.Db.NominalCodes.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId).ToListAsync(ct);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (!DateOnly.TryParse(Val(dict, "ContractStartDate"), out _))
                    {
                        Invalid(row, "ContractStartDate must be yyyy-MM-dd.");
                    }
                    else if (!clients.Any(c =>
                                 c.ReferenceNumber.Equals(Val(dict, "ClientReference"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown client reference '{Val(dict, "ClientReference")}'.");
                    }
                    else if (!authorities.Any(a =>
                                 a.Code.Equals(Val(dict, "FundingAuthorityCode"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown funding authority '{Val(dict, "FundingAuthorityCode")}'.");
                    }
                    else if (!categories.Any(c =>
                                 c.Code.Equals(Val(dict, "InvoiceCategoryCode"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown invoice category '{Val(dict, "InvoiceCategoryCode")}'.");
                    }
                    else if (!nominals.Any(n =>
                                 n.Code.Equals(Val(dict, "NominalCode"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown nominal code '{Val(dict, "NominalCode")}'.");
                    }
                    else
                    {
                        Valid(row);
                    }

                    preview.Rows.Add(row);
                }

                FinalizeCounts(preview);
                return preview;
            },
            CommitImportAsync = async (svc, preview, ct) =>
            {
                var created = 0;
                var updated = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var client = await svc.Db.Clients.FirstAsync(c =>
                        c.TenantId == svc.TenantId
                        && c.ReferenceNumber == Val(row.Values, "ClientReference"), ct);
                    var authority = await svc.Db.FundingAuthorities.FirstAsync(a =>
                        a.TenantId == svc.TenantId
                        && a.Code == Val(row.Values, "FundingAuthorityCode"), ct);
                    var category = await svc.Db.InvoiceCategories.FirstAsync(c =>
                        c.TenantId == svc.TenantId
                        && c.Code == Val(row.Values, "InvoiceCategoryCode"), ct);
                    var nominal = await svc.Db.NominalCodes.FirstAsync(n =>
                        n.TenantId == svc.TenantId
                        && n.Code == Val(row.Values, "NominalCode"), ct);
                    int? templateId = null;
                    var templateName = Val(row.Values, "InvoiceTemplateName");
                    if (!string.IsNullOrWhiteSpace(templateName))
                    {
                        templateId = await svc.Db.InvoiceTemplates
                            .Where(t => t.TenantId == svc.TenantId && t.Name == templateName)
                            .Select(t => (int?)t.Id)
                            .FirstOrDefaultAsync(ct);
                    }

                    var start = DateOnly.Parse(Val(row.Values, "ContractStartDate"), CultureInfo.InvariantCulture);
                    DateOnly? end = string.IsNullOrWhiteSpace(Val(row.Values, "ContractEndDate"))
                        ? null
                        : DateOnly.Parse(Val(row.Values, "ContractEndDate"), CultureInfo.InvariantCulture);
                    var status = string.IsNullOrWhiteSpace(Val(row.Values, "Status"))
                        ? "Active"
                        : Val(row.Values, "Status");

                    var existing = await svc.Db.ClientFundingContracts.FirstOrDefaultAsync(c =>
                        c.TenantId == svc.TenantId
                        && c.ClientId == client.Id
                        && c.FundingAuthorityId == authority.Id
                        && c.InvoiceCategoryId == category.Id, ct);

                    if (existing is null)
                    {
                        var (contract, error, _) = await svc.Funding.CreateAsync(
                            svc.TenantId,
                            client.Id,
                            new CreateFundingContractRequest
                            {
                                FundingAuthorityId = authority.Id,
                                InvoiceCategoryId = category.Id,
                                NominalCodeId = nominal.Id,
                                InvoiceTemplateId = templateId,
                                ContractStartDate = start,
                                ContractEndDate = end
                            });
                        if (error is not null || contract is null)
                        {
                            throw new InvalidOperationException(error?.Message ?? "Could not create funding contract.");
                        }

                        if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                        {
                            await svc.Funding.UpdateAsync(
                                svc.TenantId,
                                contract.Id,
                                new UpdateFundingContractRequest
                                {
                                    FundingAuthorityId = authority.Id,
                                    InvoiceCategoryId = category.Id,
                                    NominalCodeId = nominal.Id,
                                    InvoiceTemplateId = templateId,
                                    ContractStartDate = start,
                                    ContractEndDate = end,
                                    Status = status
                                });
                        }

                        created++;
                    }
                    else
                    {
                        var (_, error, _) = await svc.Funding.UpdateAsync(
                            svc.TenantId,
                            existing.Id,
                            new UpdateFundingContractRequest
                            {
                                FundingAuthorityId = authority.Id,
                                InvoiceCategoryId = category.Id,
                                NominalCodeId = nominal.Id,
                                InvoiceTemplateId = templateId,
                                ContractStartDate = start,
                                ContractEndDate = end,
                                Status = status
                            });
                        if (error is not null)
                        {
                            throw new InvalidOperationException(error.Message);
                        }

                        updated++;
                    }
                }

                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition FundingRates() => new()
        {
            Key = "funding-rates",
            Label = "Funding rates",
            SupportsImport = true,
            SupportsExport = true,
            Columns =
            [
                "ClientReference", "FundingAuthorityCode", "InvoiceCategoryCode",
                "EffectiveFrom", "EffectiveTo", "Frequency", "Amount", "Notes"
            ],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.FundingRates.AsNoTracking()
                    .Include(x => x.ClientFundingContract)
                    .ThenInclude(x => x.Client)
                    .Include(x => x.ClientFundingContract.FundingAuthority)
                    .Include(x => x.ClientFundingContract.InvoiceCategory)
                    .Where(x => x.ClientFundingContract.TenantId == svc.TenantId)
                    .OrderBy(x => x.EffectiveFrom)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("ClientReference", x.ClientFundingContract.Client.ReferenceNumber),
                    ("FundingAuthorityCode", x.ClientFundingContract.FundingAuthority.Code),
                    ("InvoiceCategoryCode", x.ClientFundingContract.InvoiceCategory.Code),
                    ("EffectiveFrom", x.EffectiveFrom.ToString("yyyy-MM-dd")),
                    ("EffectiveTo", x.EffectiveTo?.ToString("yyyy-MM-dd") ?? ""),
                    ("Frequency", x.Frequency),
                    ("Amount", x.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("Notes", x.Notes ?? ""))).ToList();
            },
            PreviewImportAsync = async (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("funding-rates", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (!DateOnly.TryParse(Val(dict, "EffectiveFrom"), out _)
                        || !decimal.TryParse(Val(dict, "Amount"), out _))
                    {
                        Invalid(row, "EffectiveFrom (yyyy-MM-dd) and Amount are required.");
                    }
                    else
                    {
                        Valid(row);
                    }

                    preview.Rows.Add(row);
                }

                FinalizeCounts(preview);
                return preview;
            },
            CommitImportAsync = async (svc, preview, ct) =>
            {
                var created = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var contract = await svc.Db.ClientFundingContracts
                        .Include(x => x.Client)
                        .Include(x => x.FundingAuthority)
                        .Include(x => x.InvoiceCategory)
                        .FirstOrDefaultAsync(c =>
                            c.TenantId == svc.TenantId
                            && c.Client.ReferenceNumber == Val(row.Values, "ClientReference")
                            && c.FundingAuthority.Code == Val(row.Values, "FundingAuthorityCode")
                            && c.InvoiceCategory.Code == Val(row.Values, "InvoiceCategoryCode"), ct)
                        ?? throw new InvalidOperationException("Funding contract not found for rate row.");

                    var from = DateOnly.Parse(Val(row.Values, "EffectiveFrom"), CultureInfo.InvariantCulture);
                    DateOnly? to = string.IsNullOrWhiteSpace(Val(row.Values, "EffectiveTo"))
                        ? null
                        : DateOnly.Parse(Val(row.Values, "EffectiveTo"), CultureInfo.InvariantCulture);
                    var amount = decimal.Parse(Val(row.Values, "Amount"), CultureInfo.InvariantCulture);
                    var (rate, error, _) = await svc.Funding.AddRateAsync(
                        svc.TenantId,
                        contract.Id,
                        new CreateFundingRateRequest
                        {
                            EffectiveFrom = from,
                            EffectiveTo = to,
                            Frequency = string.IsNullOrWhiteSpace(Val(row.Values, "Frequency"))
                                ? "Weekly"
                                : Val(row.Values, "Frequency"),
                            Amount = amount,
                            Notes = NullIfEmpty(Val(row.Values, "Notes"))
                        });
                    if (error is not null || rate is null)
                    {
                        throw new InvalidOperationException(error?.Message ?? "Could not add funding rate.");
                    }

                    created++;
                }

                return new DataTransferCommitResultDto { Created = created, Updated = 0 };
            }
        };

        public static EntityDefinition OrganisationSettings() => new()
        {
            Key = "organisation-settings",
            Label = "Organisation settings",
            SupportsImport = true,
            SupportsExport = true,
            Columns =
            [
                "CurrencyCode", "CurrencySymbol", "TimeZoneId", "InvoicePrefix", "CreditNotePrefix",
                "PaymentTermsDays", "BillingPeriodMode", "AllowPrivatePayer", "ShowGuardian", "FinanceModuleEnabled"
            ],
            ExportAsync = async (svc, ct) =>
            {
                var s = await svc.Db.TenantSettings.AsNoTracking()
                    .FirstAsync(x => x.TenantId == svc.TenantId, ct);
                return
                [
                    Row(
                        ("CurrencyCode", s.CurrencyCode),
                        ("CurrencySymbol", s.CurrencySymbol),
                        ("TimeZoneId", s.TimeZoneId),
                        ("InvoicePrefix", s.InvoicePrefix),
                        ("CreditNotePrefix", s.CreditNotePrefix),
                        ("PaymentTermsDays", s.PaymentTermsDays.ToString(CultureInfo.InvariantCulture)),
                        ("BillingPeriodMode", s.BillingPeriodMode),
                        ("AllowPrivatePayer", s.AllowPrivatePayer ? "true" : "false"),
                        ("ShowGuardian", s.ShowGuardian ? "true" : "false"),
                        ("FinanceModuleEnabled", s.FinanceModuleEnabled ? "true" : "false"))
                ];
            },
            PreviewImportAsync = (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("organisation-settings", fileName);
                if (table.Count == 0)
                {
                    preview.Rows.Add(NewRow(2, new Dictionary<string, string>()));
                    Invalid(preview.Rows[0], "At least one settings row is required.");
                }
                else
                {
                    var row = NewRow(2, table[0]);
                    Valid(row);
                    preview.Rows.Add(row);
                }

                FinalizeCounts(preview);
                return Task.FromResult(preview);
            },
            CommitImportAsync = async (svc, preview, ct) =>
            {
                var values = preview.Rows.First(x => x.IsValid).Values;
                var settings = await svc.Db.TenantSettings
                    .FirstAsync(x => x.TenantId == svc.TenantId, ct);
                settings.CurrencyCode = Val(values, "CurrencyCode");
                settings.CurrencySymbol = Val(values, "CurrencySymbol");
                settings.TimeZoneId = Val(values, "TimeZoneId");
                settings.InvoicePrefix = Val(values, "InvoicePrefix");
                settings.CreditNotePrefix = Val(values, "CreditNotePrefix");
                if (int.TryParse(Val(values, "PaymentTermsDays"), out var terms))
                {
                    settings.PaymentTermsDays = terms;
                }

                settings.BillingPeriodMode = string.IsNullOrWhiteSpace(Val(values, "BillingPeriodMode"))
                    ? settings.BillingPeriodMode
                    : Val(values, "BillingPeriodMode");
                settings.AllowPrivatePayer = ParseBool(Val(values, "AllowPrivatePayer"), settings.AllowPrivatePayer);
                settings.ShowGuardian = ParseBool(Val(values, "ShowGuardian"), settings.ShowGuardian);
                settings.FinanceModuleEnabled = ParseBool(Val(values, "FinanceModuleEnabled"), settings.FinanceModuleEnabled);
                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = 0, Updated = 1 };
            }
        };

        public static EntityDefinition CollectionsPolicy() => new()
        {
            Key = "collections-policy",
            Label = "Collections policy",
            SupportsImport = true,
            SupportsExport = true,
            Columns =
            [
                "DueReminderDaysBefore", "Overdue7Days", "Overdue14Days", "Overdue30Days", "EscalationDays",
                "RemindersEnabled", "ReminderEmailSubjectTemplate", "ReminderEmailBodyTemplate"
            ],
            ExportAsync = async (svc, ct) =>
            {
                var p = await svc.Db.CollectionPolicies.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.IsDefault, ct);
                if (p is null)
                {
                    return [];
                }

                return
                [
                    Row(
                        ("DueReminderDaysBefore", p.DueReminderDaysBefore.ToString(CultureInfo.InvariantCulture)),
                        ("Overdue7Days", p.Overdue7Days.ToString(CultureInfo.InvariantCulture)),
                        ("Overdue14Days", p.Overdue14Days.ToString(CultureInfo.InvariantCulture)),
                        ("Overdue30Days", p.Overdue30Days.ToString(CultureInfo.InvariantCulture)),
                        ("EscalationDays", p.EscalationDays.ToString(CultureInfo.InvariantCulture)),
                        ("RemindersEnabled", p.RemindersEnabled ? "true" : "false"),
                        ("ReminderEmailSubjectTemplate", p.ReminderEmailSubjectTemplate ?? ""),
                        ("ReminderEmailBodyTemplate", p.ReminderEmailBodyTemplate ?? ""))
                ];
            },
            PreviewImportAsync = (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("collections-policy", fileName);
                if (table.Count == 0)
                {
                    var row = NewRow(2, new Dictionary<string, string>());
                    Invalid(row, "Policy row required.");
                    preview.Rows.Add(row);
                }
                else
                {
                    var row = NewRow(2, table[0]);
                    Valid(row);
                    preview.Rows.Add(row);
                }

                FinalizeCounts(preview);
                return Task.FromResult(preview);
            },
            CommitImportAsync = async (svc, preview, ct) =>
            {
                var v = preview.Rows.First(x => x.IsValid).Values;
                var request = new UpdateCollectionPolicyRequest
                {
                    DueReminderDaysBefore = int.Parse(Val(v, "DueReminderDaysBefore"), CultureInfo.InvariantCulture),
                    Overdue7Days = int.Parse(Val(v, "Overdue7Days"), CultureInfo.InvariantCulture),
                    Overdue14Days = int.Parse(Val(v, "Overdue14Days"), CultureInfo.InvariantCulture),
                    Overdue30Days = int.Parse(Val(v, "Overdue30Days"), CultureInfo.InvariantCulture),
                    EscalationDays = int.Parse(Val(v, "EscalationDays"), CultureInfo.InvariantCulture),
                    RemindersEnabled = ParseBool(Val(v, "RemindersEnabled"), false),
                    ReminderEmailSubjectTemplate = Val(v, "ReminderEmailSubjectTemplate"),
                    ReminderEmailBodyTemplate = Val(v, "ReminderEmailBodyTemplate")
                };
                var error = CollectionPolicyValidator.Validate(request);
                if (error is not null)
                {
                    throw new InvalidOperationException(error);
                }

                var workflow = svc.Db.CollectionPolicies
                    .FirstOrDefault(p => p.TenantId == svc.TenantId && p.IsDefault);
                if (workflow is null)
                {
                    workflow = new CollectionPolicy
                    {
                        TenantId = svc.TenantId,
                        PublicId = Guid.NewGuid(),
                        Name = "Default",
                        IsDefault = true,
                        CreatedAt = svc.Time.GetUtcNow(),
                        UpdatedAt = svc.Time.GetUtcNow()
                    };
                    svc.Db.CollectionPolicies.Add(workflow);
                }

                workflow.DueReminderDaysBefore = Math.Max(0, request.DueReminderDaysBefore);
                workflow.Overdue7Days = request.Overdue7Days;
                workflow.Overdue14Days = request.Overdue14Days;
                workflow.Overdue30Days = request.Overdue30Days;
                workflow.EscalationDays = Math.Max(0, request.EscalationDays);
                workflow.RemindersEnabled = request.RemindersEnabled;
                workflow.ReminderEmailSubjectTemplate = NullIfEmpty(request.ReminderEmailSubjectTemplate);
                workflow.ReminderEmailBodyTemplate = NullIfEmpty(request.ReminderEmailBodyTemplate);
                workflow.UpdatedAt = svc.Time.GetUtcNow();
                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = 0, Updated = 1 };
            }
        };

        public static EntityDefinition CreditNotes() => ExportOnlyExtended(
            "credit-notes",
            "Credit notes",
            ["CreditNoteNumber", "InvoiceNumber", "CreditNoteDate", "TotalAmount", "Status", "Reason"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.CreditNotes.AsNoTracking()
                    .Include(x => x.Invoice)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.CreditNoteDate)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("CreditNoteNumber", x.CreditNoteNumber),
                    ("InvoiceNumber", x.Invoice.InvoiceNumber),
                    ("CreditNoteDate", x.CreditNoteDate.ToString("yyyy-MM-dd")),
                    ("TotalAmount", x.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("Status", x.Status),
                    ("Reason", x.Reason))).ToList();
            });

        public static EntityDefinition MiscCharges() => ExportOnlyExtended(
            "misc-charges",
            "Miscellaneous charges",
            ["ClientReference", "UsedDate", "Description", "Amount", "NominalCode"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.MiscCharges.AsNoTracking()
                    .Include(x => x.Client)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.UsedDate)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("ClientReference", x.ClientReference),
                    ("UsedDate", x.UsedDate.ToString("yyyy-MM-dd")),
                    ("Description", x.Description),
                    ("Amount", x.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("NominalCode", x.NominalCodeValue ?? ""))).ToList();
            });

        public static EntityDefinition SageExports() => ExportOnlyExtended(
            "sage-exports",
            "Sage export batches",
            ["ExportedAt", "DateFrom", "DateTo", "RecordCount", "FileName", "Status"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.SageExportBatches.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.ExportedAt)
                    .Take(2000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("ExportedAt", x.ExportedAt.ToString("O")),
                    ("DateFrom", x.DateFrom.ToString("yyyy-MM-dd")),
                    ("DateTo", x.DateTo.ToString("yyyy-MM-dd")),
                    ("RecordCount", x.RecordCount.ToString(CultureInfo.InvariantCulture)),
                    ("FileName", x.FileName),
                    ("Status", x.Status))).ToList();
            });

        public static EntityDefinition ContractRenewals() => ExportOnlyExtended(
            "contract-renewals",
            "Contract renewals",
            ["ClientReference", "FundingAuthorityCode", "CurrentEndDate", "CurrentRate", "ProposedRate", "Status"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.FundingContractRenewals.AsNoTracking()
                    .Include(x => x.Contract)
                    .ThenInclude(x => x.Client)
                    .Include(x => x.Contract.FundingAuthority)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.CurrentEndDate)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("ClientReference", x.Contract.Client.ReferenceNumber),
                    ("FundingAuthorityCode", x.Contract.FundingAuthority.Code),
                    ("CurrentEndDate", x.CurrentEndDate.ToString("yyyy-MM-dd")),
                    ("CurrentRate", x.CurrentRate.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("ProposedRate", x.ProposedRate?.ToString("0.00", CultureInfo.InvariantCulture) ?? ""),
                    ("Status", x.Status))).ToList();
            });

        public static EntityDefinition RevenueAssuranceFindings() => ExportOnlyExtended(
            "revenue-assurance-findings",
            "Revenue assurance findings",
            ["RuleCode", "Severity", "Status", "EstimatedImpact", "Explanation", "DetectedAt"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.RevenueAssuranceFindings.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.DetectedAt)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("RuleCode", x.RuleCode),
                    ("Severity", x.Severity),
                    ("Status", x.Status),
                    ("EstimatedImpact", x.EstimatedImpact?.ToString("0.00", CultureInfo.InvariantCulture) ?? ""),
                    ("Explanation", x.Explanation),
                    ("DetectedAt", x.DetectedAt.ToString("O")))).ToList();
            });

        public static EntityDefinition BankAccounts() => new()
        {
            Key = "bank-accounts",
            Label = "Bank accounts",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["Name", "BankName", "AccountReference", "Currency", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.BankAccounts.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Name)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Name", x.Name),
                    ("BankName", x.BankName ?? ""),
                    ("AccountReference", x.AccountReference ?? ""),
                    ("Currency", x.Currency),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("bank-accounts", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "Name")))
                    {
                        Invalid(row, "Name is required.");
                    }
                    else
                    {
                        Valid(row);
                    }

                    preview.Rows.Add(row);
                }

                FinalizeCounts(preview);
                return Task.FromResult(preview);
            },
            CommitImportAsync = async (svc, preview, ct) =>
            {
                var created = 0;
                var updated = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var name = Val(row.Values, "Name");
                    var account = await svc.Db.BankAccounts
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Name == name, ct);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (account is null)
                    {
                        account = new BankAccount
                        {
                            TenantId = svc.TenantId,
                            PublicId = Guid.NewGuid(),
                            Name = name,
                            BankName = NullIfEmpty(Val(row.Values, "BankName")),
                            AccountReference = NullIfEmpty(Val(row.Values, "AccountReference")),
                            Currency = string.IsNullOrWhiteSpace(Val(row.Values, "Currency")) ? "GBP" : Val(row.Values, "Currency"),
                            IsActive = isActive,
                            CreatedAt = svc.Time.GetUtcNow()
                        };
                        svc.Db.BankAccounts.Add(account);
                        created++;
                    }
                    else
                    {
                        account.BankName = NullIfEmpty(Val(row.Values, "BankName"));
                        account.AccountReference = NullIfEmpty(Val(row.Values, "AccountReference"));
                        account.Currency = string.IsNullOrWhiteSpace(Val(row.Values, "Currency")) ? account.Currency : Val(row.Values, "Currency");
                        account.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition BankTransactions() => ExportOnlyExtended(
            "bank-transactions",
            "Bank transactions",
            ["AccountName", "TransactionDate", "Amount", "Direction", "Reference", "Description", "Status"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.BankTransactions.AsNoTracking()
                    .Include(x => x.BankAccount)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.TransactionDate)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("AccountName", x.BankAccount.Name),
                    ("TransactionDate", x.TransactionDate.ToString("yyyy-MM-dd")),
                    ("Amount", x.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("Direction", x.Direction),
                    ("Reference", x.Reference ?? ""),
                    ("Description", x.Description ?? ""),
                    ("Status", x.Status))).ToList();
            });

        public static EntityDefinition RemittanceBatches() => ExportOnlyExtended(
            "remittance-batches",
            "Remittance batches",
            ["SourceFileName", "Status", "PaymentReference", "LineCount", "MatchedLineCount", "CreatedAt"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.RemittanceBatches.AsNoTracking()
                    .Include(x => x.Lines)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(2000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("SourceFileName", x.SourceFileName ?? ""),
                    ("Status", x.Status),
                    ("PaymentReference", x.PaymentReference ?? ""),
                    ("LineCount", x.Lines.Count.ToString(CultureInfo.InvariantCulture)),
                    ("MatchedLineCount", x.Lines.Count(l => l.Status == "Matched").ToString(CultureInfo.InvariantCulture)),
                    ("CreatedAt", x.CreatedAt.ToString("O")))).ToList();
            });

        public static EntityDefinition PlatformTenants() => new()
        {
            Key = "platform-tenants",
            Label = "Platform tenants",
            SupportsImport = false,
            SupportsExport = true,
            RequiresPlatform = true,
            Columns = ["Name", "TradingName", "Email", "Phone", "IsActive", "CreatedAt"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.Tenants.AsNoTracking()
                    .OrderBy(x => x.Name)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Name", x.Name),
                    ("TradingName", x.TradingName ?? ""),
                    ("Email", x.Email ?? ""),
                    ("Phone", x.Phone ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"),
                    ("CreatedAt", x.CreatedAt.ToString("O")))).ToList();
            },
            PreviewImportAsync = (_, _, _, _) => throw new InvalidOperationException(),
            CommitImportAsync = (_, _, _) => throw new InvalidOperationException()
        };

        private static EntityDefinition ExportOnlyExtended(
            string key,
            string label,
            string[] columns,
            Func<DataTransferService, CancellationToken, Task<List<Dictionary<string, string>>>> export) => new()
        {
            Key = key,
            Label = label,
            SupportsImport = false,
            SupportsExport = true,
            Columns = columns,
            ExportAsync = export,
            PreviewImportAsync = (_, _, _, _) => throw new InvalidOperationException(),
            CommitImportAsync = (_, _, _) => throw new InvalidOperationException()
        };
    }
}
