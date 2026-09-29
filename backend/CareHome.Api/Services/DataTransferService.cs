using System.Globalization;
using CareHome.Api.Audit;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.DataTransfer;
using CareHome.Api.ImportExport;
using CareHome.Api.Funding;
using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Services;

public sealed partial class DataTransferService(
    CareHomeDbContext dbContext,
    ITenantContext tenantContext,
    AuditService audit,
    UserAccessService userAccess,
    TimeProvider timeProvider,
    FundingContractService fundingContracts)
{
    private static readonly IReadOnlyDictionary<string, EntityDefinition> Definitions = BuildDefinitions();

    public IReadOnlyList<DataTransferEntityInfoDto> ListEntities() =>
        Definitions.Values
            .Select(d => new DataTransferEntityInfoDto
            {
                Key = d.Key,
                Label = d.Label,
                SupportsImport = d.SupportsImport,
                SupportsExport = d.SupportsExport,
                Columns = d.Columns,
                RequiresPlatform = d.RequiresPlatform
            })
            .OrderBy(x => x.Label)
            .ToList();

    public async Task<byte[]> GetTemplateAsync(string entity, string format, CancellationToken cancellationToken)
    {
        var def = RequireDefinition(entity);
        if (!def.SupportsImport)
        {
            throw new InvalidOperationException("This entity does not support import.");
        }

        var example = def.BuildExampleRow();
        return TabularSpreadsheet.Write(def.Columns, [example], format);
    }

    public async Task<byte[]> ExportAsync(string entity, string format, CancellationToken cancellationToken)
    {
        var def = RequireDefinition(entity);
        if (!def.SupportsExport)
        {
            throw new InvalidOperationException("This entity does not support export.");
        }

        var rows = await def.ExportAsync(this, cancellationToken);
        return TabularSpreadsheet.Write(def.Columns, rows, format);
    }

    public async Task<DataTransferPreviewDto> PreviewImportAsync(
        string entity,
        string fileName,
        Stream stream,
        CancellationToken cancellationToken)
    {
        var def = RequireDefinition(entity);
        if (!def.SupportsImport)
        {
            throw new InvalidOperationException("This entity does not support import.");
        }

        if (!TabularSpreadsheet.IsSupported(fileName))
        {
            throw new InvalidOperationException("Only .csv and .xlsx files are supported.");
        }

        var tableRows = TabularSpreadsheet.ReadRows(stream, fileName);
        return await def.PreviewImportAsync(this, fileName, tableRows, cancellationToken);
    }

    public async Task<DataTransferCommitResultDto> CommitImportAsync(
        string entity,
        DataTransferPreviewDto preview,
        CancellationToken cancellationToken)
    {
        var def = RequireDefinition(entity);
        if (!def.SupportsImport)
        {
            throw new InvalidOperationException("This entity does not support import.");
        }

        if (preview.Rows.Any(x => !x.IsValid))
        {
            throw new InvalidOperationException("Invalid rows must be corrected before import.");
        }

        return await def.CommitImportAsync(this, preview, cancellationToken);
    }

    private EntityDefinition RequireDefinition(string entity)
    {
        var key = entity.Trim().ToLowerInvariant();
        if (!Definitions.TryGetValue(key, out var def))
        {
            throw new InvalidOperationException($"Unknown data transfer entity '{entity}'.");
        }

        return def;
    }

    internal int TenantId => tenantContext.TenantId;

    internal CareHomeDbContext Db => dbContext;

    internal AuditService Audit => audit;

    internal UserAccessService UserAccess => userAccess;

    internal TimeProvider Time => timeProvider;

    internal FundingContractService Funding => fundingContracts;

    internal sealed class EntityDefinition
    {
        public required string Key { get; init; }

        public required string Label { get; init; }

        public required bool SupportsImport { get; init; }

        public required bool SupportsExport { get; init; }

        public required string[] Columns { get; init; }

        public bool RequiresPlatform { get; init; }

        public required Func<DataTransferService, CancellationToken, Task<List<Dictionary<string, string>>>> ExportAsync { get; init; }

        public required Func<DataTransferService, string, List<Dictionary<string, string>>, CancellationToken, Task<DataTransferPreviewDto>> PreviewImportAsync { get; init; }

        public required Func<DataTransferService, DataTransferPreviewDto, CancellationToken, Task<DataTransferCommitResultDto>> CommitImportAsync { get; init; }

        public Dictionary<string, string> BuildExampleRow()
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in Columns)
            {
                row[col] = col switch
                {
                    "Name" => "Example Ltd",
                    "Code" => "EX01",
                    "CompanyName" => "Example Ltd",
                    "CareHomeCode" => "RVH",
                    "ReferenceNumber" => "RES-001",
                    "SageId" => "S001",
                    "FirstName" => "Alex",
                    "LastName" => "Morgan",
                    "CareType" => "Residential",
                    "Status" => "Current",
                    "AdmissionDate" => "2026-01-15",
                    "Type" => "Council",
                    "BillingFrequency" => "Monthly",
                    "Description" => "General care",
                    "IsActive" => "true",
                    "Email" => "finance@example.com",
                    "Phone" => "01234567890",
                    "Address" => "1 High Street",
                    "BedCapacity" => "40",
                    "InvoiceNumber" => "INV-0001",
                    "PaymentReference" => "PAY-001",
                    "Amount" => "1000.00",
                    "PaymentDate" => "2026-03-01",
                    "UserEmail" => "user@example.com",
                    "Roles" => "ReadOnly",
                    "Action" => "Login",
                    "AttemptedAt" => DateTime.UtcNow.ToString("O"),
                    "Recipient" => "funder@example.com",
                    "DocumentType" => "Invoice",
                    "Success" => "true",
                    _ => string.Empty
                };
            }

            return row;
        }
    }

    private static IReadOnlyDictionary<string, EntityDefinition> BuildDefinitions()
    {
        var list = new List<EntityDefinition>
        {
            MasterData.Companies(),
            MasterData.CareHomes(),
            MasterData.FundingAuthorities(),
            MasterData.NominalCodes(),
            MasterData.InvoiceCategories(),
            MasterData.Clients(),
            MasterData.InvoiceTemplates(),
            Exports.Invoices(),
            Exports.Payments(),
            Exports.Disputes(),
            Exports.Audit(),
            Exports.EmailSendLogs(),
            Exports.Users(),
        };

        list.AddRange(ExtendedCatalog.Definitions());

        return list.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
    }

    private static class MasterData
    {
        public static EntityDefinition Companies() => new()
        {
            Key = "companies",
            Label = "Companies",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["Name", "Address", "Phone", "Email", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.Companies.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Name)
                    .Select(x => new { x.Name, x.Address, x.Phone, x.Email, x.IsActive })
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Name", x.Name),
                    ("Address", x.Address ?? ""),
                    ("Phone", x.Phone ?? ""),
                    ("Email", x.Email ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = async (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("companies", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    var name = Val(dict, "Name");
                    if (string.IsNullOrWhiteSpace(name))
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
                return preview;
            },
            CommitImportAsync = async (svc, preview, ct) =>
            {
                var created = 0;
                var updated = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var name = Val(row.Values, "Name");
                    var entity = await svc.Db.Companies
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Name == name, ct);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (entity is null)
                    {
                        entity = new Company
                        {
                            TenantId = svc.TenantId,
                            PublicId = Guid.NewGuid(),
                            Name = name,
                            Address = NullIfEmpty(Val(row.Values, "Address")),
                            Phone = NullIfEmpty(Val(row.Values, "Phone")),
                            Email = NullIfEmpty(Val(row.Values, "Email")),
                            IsActive = isActive
                        };
                        svc.Db.Companies.Add(entity);
                        created++;
                    }
                    else
                    {
                        entity.Address = NullIfEmpty(Val(row.Values, "Address"));
                        entity.Phone = NullIfEmpty(Val(row.Values, "Phone"));
                        entity.Email = NullIfEmpty(Val(row.Values, "Email"));
                        entity.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                await svc.Audit.LogAsync("DataTransfer", "companies", "Import", null, new { created, updated }, "Imported companies.", ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition CareHomes() => new()
        {
            Key = "care-homes",
            Label = "Care homes",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["CompanyName", "Code", "Name", "Address", "BedCapacity", "Phone", "Email", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var homes = await svc.Db.CareHomes.AsNoTracking()
                    .Include(x => x.Company)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Name)
                    .ToListAsync(ct);
                return homes.Select(x => Row(
                    ("CompanyName", x.Company?.Name ?? ""),
                    ("Code", x.Code),
                    ("Name", x.Name),
                    ("Address", x.Address ?? ""),
                    ("BedCapacity", x.BedCapacity.ToString(CultureInfo.InvariantCulture)),
                    ("Phone", x.Phone ?? ""),
                    ("Email", x.Email ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = async (svc, fileName, table, ct) =>
            {
                var companies = await svc.Db.Companies.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var preview = NewPreview("care-homes", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "Code")) || string.IsNullOrWhiteSpace(Val(dict, "Name")))
                    {
                        Invalid(row, "Code and Name are required.");
                    }
                    else if (!companies.Any(c => c.Name.Equals(Val(dict, "CompanyName"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown company '{Val(dict, "CompanyName")}'.");
                    }
                    else if (!int.TryParse(Val(dict, "BedCapacity"), out _))
                    {
                        Invalid(row, "BedCapacity must be a number.");
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
                var companies = await svc.Db.Companies
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var created = 0;
                var updated = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var code = Val(row.Values, "Code");
                    var company = companies.First(c =>
                        c.Name.Equals(Val(row.Values, "CompanyName"), StringComparison.OrdinalIgnoreCase));
                    var home = await svc.Db.CareHomes
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Code == code, ct);
                    var capacity = int.Parse(Val(row.Values, "BedCapacity"), CultureInfo.InvariantCulture);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (home is null)
                    {
                        home = new CareHomeLocation
                        {
                            TenantId = svc.TenantId,
                            PublicId = Guid.NewGuid(),
                            CompanyId = company.Id,
                            Code = code,
                            Name = Val(row.Values, "Name"),
                            Address = NullIfEmpty(Val(row.Values, "Address")),
                            BedCapacity = capacity,
                            Phone = NullIfEmpty(Val(row.Values, "Phone")),
                            Email = NullIfEmpty(Val(row.Values, "Email")),
                            IsActive = isActive
                        };
                        svc.Db.CareHomes.Add(home);
                        created++;
                    }
                    else
                    {
                        home.CompanyId = company.Id;
                        home.Name = Val(row.Values, "Name");
                        home.Address = NullIfEmpty(Val(row.Values, "Address"));
                        home.BedCapacity = capacity;
                        home.Phone = NullIfEmpty(Val(row.Values, "Phone"));
                        home.Email = NullIfEmpty(Val(row.Values, "Email"));
                        home.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition FundingAuthorities() => new()
        {
            Key = "funding-authorities",
            Label = "Funding authorities",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["Code", "Name", "Type", "BillingFrequency", "ContactName", "Email", "Phone", "Address", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.FundingAuthorities.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Name)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Code", x.Code),
                    ("Name", x.Name),
                    ("Type", x.Type),
                    ("BillingFrequency", x.BillingFrequency),
                    ("ContactName", x.ContactName ?? ""),
                    ("Email", x.Email ?? ""),
                    ("Phone", x.Phone ?? ""),
                    ("Address", x.Address ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("funding-authorities", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "Code")) || string.IsNullOrWhiteSpace(Val(dict, "Name")))
                    {
                        Invalid(row, "Code and Name are required.");
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
                    var code = Val(row.Values, "Code");
                    var entity = await svc.Db.FundingAuthorities
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Code == code, ct);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (entity is null)
                    {
                        entity = new FundingAuthority
                        {
                            TenantId = svc.TenantId,
                            PublicId = Guid.NewGuid(),
                            Code = code,
                            Name = Val(row.Values, "Name"),
                            Type = string.IsNullOrWhiteSpace(Val(row.Values, "Type")) ? "Council" : Val(row.Values, "Type"),
                            BillingFrequency = string.IsNullOrWhiteSpace(Val(row.Values, "BillingFrequency"))
                                ? "Monthly"
                                : Val(row.Values, "BillingFrequency"),
                            ContactName = NullIfEmpty(Val(row.Values, "ContactName")),
                            Email = NullIfEmpty(Val(row.Values, "Email")),
                            Phone = NullIfEmpty(Val(row.Values, "Phone")),
                            Address = NullIfEmpty(Val(row.Values, "Address")),
                            IsActive = isActive
                        };
                        svc.Db.FundingAuthorities.Add(entity);
                        created++;
                    }
                    else
                    {
                        entity.Name = Val(row.Values, "Name");
                        entity.Type = string.IsNullOrWhiteSpace(Val(row.Values, "Type")) ? entity.Type : Val(row.Values, "Type");
                        entity.BillingFrequency = string.IsNullOrWhiteSpace(Val(row.Values, "BillingFrequency"))
                            ? entity.BillingFrequency
                            : Val(row.Values, "BillingFrequency");
                        entity.ContactName = NullIfEmpty(Val(row.Values, "ContactName"));
                        entity.Email = NullIfEmpty(Val(row.Values, "Email"));
                        entity.Phone = NullIfEmpty(Val(row.Values, "Phone"));
                        entity.Address = NullIfEmpty(Val(row.Values, "Address"));
                        entity.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition NominalCodes() => new()
        {
            Key = "nominal-codes",
            Label = "Nominal codes",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["Code", "Name", "Description", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.NominalCodes.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Code)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Code", x.Code),
                    ("Name", x.Name),
                    ("Description", x.Description ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("nominal-codes", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "Code")) || string.IsNullOrWhiteSpace(Val(dict, "Name")))
                    {
                        Invalid(row, "Code and Name are required.");
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
                    var code = Val(row.Values, "Code");
                    var entity = await svc.Db.NominalCodes
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Code == code, ct);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (entity is null)
                    {
                        entity = new NominalCode
                        {
                            TenantId = svc.TenantId,
                            Code = code,
                            Name = Val(row.Values, "Name"),
                            Description = NullIfEmpty(Val(row.Values, "Description")),
                            IsActive = isActive
                        };
                        svc.Db.NominalCodes.Add(entity);
                        created++;
                    }
                    else
                    {
                        entity.Name = Val(row.Values, "Name");
                        entity.Description = NullIfEmpty(Val(row.Values, "Description"));
                        entity.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition InvoiceCategories() => new()
        {
            Key = "invoice-categories",
            Label = "Invoice categories",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["Code", "Name", "Description", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.InvoiceCategories.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Code)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Code", x.Code),
                    ("Name", x.Name),
                    ("Description", x.Description ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = (svc, fileName, table, ct) =>
            {
                var preview = NewPreview("invoice-categories", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "Code")) || string.IsNullOrWhiteSpace(Val(dict, "Name")))
                    {
                        Invalid(row, "Code and Name are required.");
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
                    var code = Val(row.Values, "Code");
                    var entity = await svc.Db.InvoiceCategories
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Code == code, ct);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (entity is null)
                    {
                        entity = new InvoiceCategory
                        {
                            TenantId = svc.TenantId,
                            Code = code,
                            Name = Val(row.Values, "Name"),
                            Description = NullIfEmpty(Val(row.Values, "Description")),
                            IsActive = isActive
                        };
                        svc.Db.InvoiceCategories.Add(entity);
                        created++;
                    }
                    else
                    {
                        entity.Name = Val(row.Values, "Name");
                        entity.Description = NullIfEmpty(Val(row.Values, "Description"));
                        entity.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition Clients() => new()
        {
            Key = "clients",
            Label = "Residents",
            SupportsImport = true,
            SupportsExport = true,
            Columns =
            [
                "CareHomeCode", "ReferenceNumber", "SageId", "FirstName", "LastName", "CareType", "Status",
                "AdmissionDate", "Email", "Phone"
            ],
            ExportAsync = async (svc, ct) =>
            {
                var allowed = await svc.UserAccess.GetScopedCareHomeIdsAsync(svc.TenantId, ct);
                var allowedSet = allowed.ToHashSet();
                var query = svc.Db.Clients.AsNoTracking()
                    .Include(x => x.CareHome)
                    .Where(x => x.TenantId == svc.TenantId && !x.IsArchived
                        && allowedSet.Contains(x.CareHomeId));

                var rows = await query.OrderBy(x => x.ReferenceNumber).ToListAsync(ct);
                return rows.Select(x => Row(
                    ("CareHomeCode", x.CareHome.Code),
                    ("ReferenceNumber", x.ReferenceNumber),
                    ("SageId", x.SageId),
                    ("FirstName", x.FirstName),
                    ("LastName", x.LastName),
                    ("CareType", x.CareType),
                    ("Status", x.Status),
                    ("AdmissionDate", x.AdmissionDate.ToString("yyyy-MM-dd")),
                    ("Email", x.Email ?? ""),
                    ("Phone", x.Phone ?? ""))).ToList();
            },
            PreviewImportAsync = async (svc, fileName, table, ct) =>
            {
                var homes = await svc.Db.CareHomes.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var preview = NewPreview("clients", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "ReferenceNumber"))
                        || string.IsNullOrWhiteSpace(Val(dict, "FirstName"))
                        || string.IsNullOrWhiteSpace(Val(dict, "LastName")))
                    {
                        Invalid(row, "ReferenceNumber, FirstName and LastName are required.");
                    }
                    else if (!homes.Any(h => h.Code.Equals(Val(dict, "CareHomeCode"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown care home code '{Val(dict, "CareHomeCode")}'.");
                    }
                    else if (!DateOnly.TryParse(Val(dict, "AdmissionDate"), out _))
                    {
                        Invalid(row, "AdmissionDate must be yyyy-MM-dd.");
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
                var homes = await svc.Db.CareHomes
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var created = 0;
                var updated = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var reference = Val(row.Values, "ReferenceNumber");
                    var home = homes.First(h =>
                        h.Code.Equals(Val(row.Values, "CareHomeCode"), StringComparison.OrdinalIgnoreCase));
                    var client = await svc.Db.Clients
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.ReferenceNumber == reference, ct);
                    var admission = DateOnly.Parse(Val(row.Values, "AdmissionDate"), CultureInfo.InvariantCulture);
                    if (client is null)
                    {
                        client = new Client
                        {
                            TenantId = svc.TenantId,
                            PublicId = Guid.NewGuid(),
                            CareHomeId = home.Id,
                            ReferenceNumber = reference,
                            SageId = string.IsNullOrWhiteSpace(Val(row.Values, "SageId"))
                                ? reference
                                : Val(row.Values, "SageId"),
                            FirstName = Val(row.Values, "FirstName"),
                            LastName = Val(row.Values, "LastName"),
                            CareType = string.IsNullOrWhiteSpace(Val(row.Values, "CareType"))
                                ? "Residential"
                                : Val(row.Values, "CareType"),
                            Status = string.IsNullOrWhiteSpace(Val(row.Values, "Status"))
                                ? "Current"
                                : Val(row.Values, "Status"),
                            AdmissionDate = admission,
                            Email = NullIfEmpty(Val(row.Values, "Email")),
                            Phone = NullIfEmpty(Val(row.Values, "Phone"))
                        };
                        svc.Db.Clients.Add(client);
                        created++;
                    }
                    else
                    {
                        client.CareHomeId = home.Id;
                        client.FirstName = Val(row.Values, "FirstName");
                        client.LastName = Val(row.Values, "LastName");
                        client.CareType = string.IsNullOrWhiteSpace(Val(row.Values, "CareType"))
                            ? client.CareType
                            : Val(row.Values, "CareType");
                        client.Status = string.IsNullOrWhiteSpace(Val(row.Values, "Status"))
                            ? client.Status
                            : Val(row.Values, "Status");
                        client.AdmissionDate = admission;
                        client.Email = NullIfEmpty(Val(row.Values, "Email"));
                        client.Phone = NullIfEmpty(Val(row.Values, "Phone"));
                        if (!string.IsNullOrWhiteSpace(Val(row.Values, "SageId")))
                        {
                            client.SageId = Val(row.Values, "SageId");
                        }

                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };

        public static EntityDefinition InvoiceTemplates() => new()
        {
            Key = "invoice-templates",
            Label = "Invoice templates",
            SupportsImport = true,
            SupportsExport = true,
            Columns = ["Name", "InvoiceCategoryCode", "EmailSubjectTemplate", "IsActive"],
            ExportAsync = async (svc, ct) =>
            {
                var rows = await svc.Db.InvoiceTemplates.AsNoTracking()
                    .Include(x => x.InvoiceCategory)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Name)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Name", x.Name),
                    ("InvoiceCategoryCode", x.InvoiceCategory?.Code ?? ""),
                    ("EmailSubjectTemplate", x.EmailSubjectTemplate ?? ""),
                    ("IsActive", x.IsActive ? "true" : "false"))).ToList();
            },
            PreviewImportAsync = async (svc, fileName, table, ct) =>
            {
                var categories = await svc.Db.InvoiceCategories.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var preview = NewPreview("invoice-templates", fileName);
                foreach (var (i, dict) in table.Select((r, i) => (i, r)))
                {
                    var row = NewRow(i + 2, dict);
                    if (string.IsNullOrWhiteSpace(Val(dict, "Name")))
                    {
                        Invalid(row, "Name is required.");
                    }
                    else if (!categories.Any(c =>
                                 c.Code.Equals(Val(dict, "InvoiceCategoryCode"), StringComparison.OrdinalIgnoreCase)))
                    {
                        Invalid(row, $"Unknown invoice category code '{Val(dict, "InvoiceCategoryCode")}'.");
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
                var categories = await svc.Db.InvoiceCategories
                    .Where(x => x.TenantId == svc.TenantId)
                    .ToListAsync(ct);
                var created = 0;
                var updated = 0;
                foreach (var row in preview.Rows.Where(x => x.IsValid))
                {
                    var name = Val(row.Values, "Name");
                    var category = categories.First(c =>
                        c.Code.Equals(Val(row.Values, "InvoiceCategoryCode"), StringComparison.OrdinalIgnoreCase));
                    var template = await svc.Db.InvoiceTemplates
                        .FirstOrDefaultAsync(x => x.TenantId == svc.TenantId && x.Name == name, ct);
                    var isActive = ParseBool(Val(row.Values, "IsActive"), true);
                    if (template is null)
                    {
                        template = new InvoiceTemplate
                        {
                            TenantId = svc.TenantId,
                            Name = name,
                            InvoiceCategoryId = category.Id,
                            EmailSubjectTemplate = NullIfEmpty(Val(row.Values, "EmailSubjectTemplate")),
                            IsActive = isActive
                        };
                        svc.Db.InvoiceTemplates.Add(template);
                        created++;
                    }
                    else
                    {
                        template.InvoiceCategoryId = category.Id;
                        template.EmailSubjectTemplate = NullIfEmpty(Val(row.Values, "EmailSubjectTemplate"));
                        template.IsActive = isActive;
                        updated++;
                    }
                }

                await svc.Db.SaveChangesAsync(ct);
                return new DataTransferCommitResultDto { Created = created, Updated = updated };
            }
        };
    }

    private static class Exports
    {
        public static EntityDefinition Invoices() => ExportOnly(
            "invoices",
            "Invoices",
            ["InvoiceNumber", "CareHomeName", "FunderName", "InvoiceDate", "DueDate", "TotalAmount", "Status", "PaymentStatus"],
            async (svc, ct) =>
            {
                var homes = await svc.UserAccess.GetScopedCareHomeIdsAsync(svc.TenantId, ct);
                var homeSet = homes.ToHashSet();
                var query = svc.Db.Invoices.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId && x.Status != "Void"
                        && homeSet.Contains(x.CareHomeId));

                var rows = await query
                    .OrderByDescending(x => x.InvoiceDate)
                    .Select(x => new
                    {
                        x.InvoiceNumber,
                        x.SnapshotCareHomeName,
                        x.SnapshotFundingAuthorityName,
                        x.InvoiceDate,
                        x.DueDate,
                        x.TotalAmount,
                        x.Status,
                        x.PaymentStatus
                    })
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("InvoiceNumber", x.InvoiceNumber),
                    ("CareHomeName", x.SnapshotCareHomeName),
                    ("FunderName", x.SnapshotFundingAuthorityName),
                    ("InvoiceDate", x.InvoiceDate.ToString("yyyy-MM-dd")),
                    ("DueDate", x.DueDate.ToString("yyyy-MM-dd")),
                    ("TotalAmount", x.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("Status", x.Status),
                    ("PaymentStatus", x.PaymentStatus))).ToList();
            });

        public static EntityDefinition Payments() => ExportOnly(
            "payments",
            "Payments",
            ["Reference", "ReceivedDate", "Amount", "Source", "Status"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.Payments.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.ReceivedDate)
                    .Take(5000)
                    .Select(x => new { x.Reference, x.ReceivedDate, x.Amount, x.Source, x.Status })
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("Reference", x.Reference ?? ""),
                    ("ReceivedDate", x.ReceivedDate.ToString("yyyy-MM-dd")),
                    ("Amount", x.Amount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("Source", x.Source),
                    ("Status", x.Status))).ToList();
            });

        public static EntityDefinition Disputes() => ExportOnly(
            "disputes",
            "Disputes",
            ["InvoiceNumber", "FunderName", "DisputedAmount", "ReasonCode", "Status"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.InvoiceDisputes.AsNoTracking()
                    .Include(x => x.Invoice)
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.OpenedDate)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("InvoiceNumber", x.Invoice.InvoiceNumber),
                    ("FunderName", x.Invoice.SnapshotFundingAuthorityName),
                    ("DisputedAmount", x.DisputedAmount.ToString("0.00", CultureInfo.InvariantCulture)),
                    ("ReasonCode", x.ReasonCode),
                    ("Status", x.Status))).ToList();
            });

        public static EntityDefinition Audit() => ExportOnly(
            "audit",
            "Audit log",
            ["LoggedAt", "UserId", "EntityType", "EntityId", "Action", "Description"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.AuditLogs.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.LoggedAt)
                    .Take(5000)
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("LoggedAt", x.LoggedAt.ToString("O")),
                    ("UserId", x.UserId ?? ""),
                    ("EntityType", x.EntityType),
                    ("EntityId", x.EntityId ?? ""),
                    ("Action", x.Action),
                    ("Description", x.Description ?? ""))).ToList();
            });

        public static EntityDefinition EmailSendLogs() => ExportOnly(
            "email-send-logs",
            "Email delivery log",
            ["AttemptedAt", "DocumentType", "Recipient", "Success", "ErrorMessage"],
            async (svc, ct) =>
            {
                var rows = await svc.Db.EmailSendLogs.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderByDescending(x => x.AttemptedAt)
                    .Take(5000)
                    .Select(x => new { x.AttemptedAt, x.DocumentType, x.Recipient, x.Success, x.ErrorMessage })
                    .ToListAsync(ct);
                return rows.Select(x => Row(
                    ("AttemptedAt", x.AttemptedAt.ToString("O")),
                    ("DocumentType", x.DocumentType),
                    ("Recipient", x.Recipient ?? ""),
                    ("Success", x.Success ? "true" : "false"),
                    ("ErrorMessage", x.ErrorMessage ?? ""))).ToList();
            });

        public static EntityDefinition Users() => ExportOnly(
            "users",
            "Users",
            ["Email", "Roles", "IsActive"],
            async (svc, ct) =>
            {
                var users = await svc.Db.Users.AsNoTracking()
                    .Where(x => x.TenantId == svc.TenantId)
                    .OrderBy(x => x.Email)
                    .ToListAsync(ct);
                var result = new List<Dictionary<string, string>>();
                foreach (var user in users)
                {
                    var roles = await svc.Db.UserRoles
                        .Where(ur => ur.UserId == user.Id)
                        .Join(svc.Db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name)
                        .ToListAsync(ct);
                    result.Add(Row(
                        ("Email", user.Email ?? ""),
                        ("Roles", string.Join(';', roles)),
                        ("IsActive", user.IsActive ? "true" : "false")));
                }

                return result;
            });

        private static EntityDefinition ExportOnly(
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

    private static DataTransferPreviewDto NewPreview(string entity, string fileName) =>
        new() { Entity = entity, FileName = fileName };

    private static DataTransferPreviewRowDto NewRow(int rowNumber, Dictionary<string, string> values) =>
        new() { RowNumber = rowNumber, Values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase) };

    private static void Valid(DataTransferPreviewRowDto row)
    {
        row.IsValid = true;
        row.Error = null;
    }

    private static void Invalid(DataTransferPreviewRowDto row, string error)
    {
        row.IsValid = false;
        row.Error = error;
    }

    private static void FinalizeCounts(DataTransferPreviewDto preview)
    {
        preview.ValidCount = preview.Rows.Count(x => x.IsValid);
        preview.InvalidCount = preview.Rows.Count(x => !x.IsValid);
    }

    private static Dictionary<string, string> Row(params (string Key, string Value)[] pairs)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            dict[key] = value;
        }

        return dict;
    }

    private static string Val(IReadOnlyDictionary<string, string> dict, string key) =>
        dict.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ParseBool(string value, bool defaultValue) =>
        string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : value.Equals("true", StringComparison.OrdinalIgnoreCase)
              || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
              || value == "1";
}
