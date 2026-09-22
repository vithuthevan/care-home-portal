using CareHome.Api.Abstractions;
using CareHome.Api.Billing;
using CareHome.Billing.Billing;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Dtos.Billing;
using CareHome.Api.Funding;
using CareHome.Api.Models;
using CareHome.Api.Telemetry;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Billing
{
    public class BillingService(
        CareHomeDbContext dbContext,
        RateCalculator rateCalculator,
        InvoiceTemplateResolver templateResolver,
        IDocumentSequence sequences,
        ICareHomeAccessScope userAccess,
        IAuditWriter audit,
        ILogger<BillingService> logger)
    {
        public async Task<BillingPreviewResponse> PreviewAsync(
            int tenantId,
            BillingPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            var (lines, exceptions, coverage) = await BuildPreviewAsync(tenantId, request, cancellationToken);

            var critical = exceptions.Any(x => x.Severity == "Error");
            var total = Money.Round(lines.Sum(x => x.Amount));

            return new BillingPreviewResponse
            {
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                RequestedPeriodStart = request.PeriodStart,
                RequestedPeriodEnd = request.PeriodEnd,
                SkippedAlreadyBilledDays = coverage.Sum(x => x.SkippedAlreadyBilledDays),
                Coverage = coverage,
                Lines = lines,
                Exceptions = exceptions,
                TotalAmount = total,
                CanGenerate = !critical && lines.Count > 0
            };
        }

        public async Task<(BillingGenerateResponse? Result, string? Error)> GenerateAsync(
            int tenantId,
            BillingPreviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.PeriodEnd < request.PeriodStart)
            {
                return (null, "Billing period end cannot be before start.");
            }

            CareHomeTelemetry.BillingGenerateAttempts.Add(1);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Billing generate started. TenantId={TenantId} CompanyId={CompanyId} CareHomeId={CareHomeId} Period={PeriodStart:yyyy-MM-dd}/{PeriodEnd:yyyy-MM-dd}",
                    tenantId,
                    request.CompanyId,
                    request.CareHomeId,
                    request.PeriodStart,
                    request.PeriodEnd);
            }

            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await AcquireBillingLockAsync(tenantId, cancellationToken);

            var (lines, exceptions, _) = await BuildPreviewAsync(tenantId, request, cancellationToken);
            var critical = exceptions.Where(x => x.Severity == "Error").ToList();

            foreach (var exception in exceptions)
            {
                dbContext.BillingExceptionLogs.Add(new BillingExceptionLog
                {
                    TenantId = tenantId,
                    LoggedAt = DateTimeOffset.UtcNow,
                    ClientId = exception.ClientId,
                    CareHomeId = exception.CareHomeId,
                    ClientFundingContractId = exception.ClientFundingContractId,
                    Severity = exception.Severity,
                    Code = exception.Code,
                    Message = exception.Message,
                    PeriodStart = request.PeriodStart,
                    PeriodEnd = request.PeriodEnd
                });
            }

            if (critical.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                var blocked = critical.Any(x => x.Code == "ALREADY_FULLY_BILLED")
                    ? "The requested period is already fully billed. No unbilled service dates remain."
                    : "Billing generation was blocked because of critical errors.";
                CareHomeTelemetry.BillingGenerateFailures.Add(1);
                logger.LogWarning(
                    "Billing generate blocked. TenantId={TenantId} Reason={Reason} Errors={ErrorCount}",
                    tenantId,
                    blocked,
                    critical.Count);
                return (new BillingGenerateResponse
                {
                    Exceptions = exceptions
                }, blocked);
            }

            if (lines.Count == 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return (null, "There is nothing to invoice for the selected criteria.");
            }

            var now = DateTimeOffset.UtcNow;
            var invoiceDate = request.PeriodEnd;
            var settings = await dbContext.TenantSettings.AsNoTracking()
                .FirstAsync(x => x.TenantId == tenantId, cancellationToken);
            var tenant = await dbContext.Tenants.AsNoTracking()
                .FirstAsync(x => x.Id == tenantId, cancellationToken);
            var dueDate = invoiceDate.AddDays(settings.PaymentTermsDays);
            var createdIds = new List<int>();

            var groups = lines.GroupBy(x => new
            {
                x.CompanyId,
                x.CareHomeId,
                x.FundingAuthorityId,
                x.InvoiceCategoryId
            });

            foreach (var group in groups)
            {
                foreach (var line in group)
                {
                    var overlap = await HasFinalizedOverlapAsync(
                        tenantId,
                        line.ClientId,
                        line.ClientFundingContractId,
                        line.InvoiceCategoryId,
                        line.ServiceFrom,
                        line.ServiceTo,
                        cancellationToken);

                    if (overlap)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return (null, $"Duplicate billing detected for client {line.ClientName} between {line.ServiceFrom:yyyy-MM-dd} and {line.ServiceTo:yyyy-MM-dd}.");
                    }
                }

                var first = group.First();
                var template = await templateResolver.ResolveAsync(
                    tenantId,
                    first.InvoiceCategoryId,
                    first.FundingAuthorityId,
                    first.CareHomeId,
                    first.CompanyId,
                    cancellationToken);

                if (template is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return (null, $"No invoice template is configured for category '{first.InvoiceCategoryName}'.");
                }

                var invoiceNumber = await sequences.NextAsync(tenantId, DocumentTypes.Invoice, cancellationToken);

                var invoice = new Invoice
                {
                    TenantId = tenantId,
                    InvoiceNumber = invoiceNumber,
                    CompanyId = first.CompanyId,
                    CareHomeId = first.CareHomeId,
                    FundingAuthorityId = first.FundingAuthorityId,
                    InvoiceCategoryId = first.InvoiceCategoryId,
                    InvoiceTemplateId = template.Id,
                    InvoiceDate = invoiceDate,
                    DueDate = dueDate,
                    PeriodStart = request.PeriodStart,
                    PeriodEnd = request.PeriodEnd,
                    Status = InvoiceStatuses.Generated,
                    PaymentStatus = "NotPaid",
                    CreatedAt = now,
                    GeneratedAt = now,
                    RecipientEmail = template.ContactEmail,
                    SnapshotTenantName = tenant.Name,
                    SnapshotCompanyName = first.CompanyName,
                    SnapshotCareHomeName = first.CareHomeName,
                    SnapshotCareHomeCode = first.CareHomeName,
                    SnapshotFundingAuthorityName = first.FundingAuthorityName,
                    SnapshotFundingAuthorityCode = first.FundingAuthorityName,
                    SnapshotInvoiceCategoryName = first.InvoiceCategoryName,
                    SnapshotInvoiceCategoryCode = first.InvoiceCategoryName,
                    SnapshotTemplateName = template.Name,
                    SnapshotHeaderText1 = template.HeaderText1,
                    SnapshotHeaderText2 = template.HeaderText2,
                    SnapshotFooterText = template.FooterText,
                    SnapshotBankAccountName = template.BankAccountName,
                    SnapshotSortCode = template.SortCode,
                    SnapshotAccountNumber = template.AccountNumber,
                    SnapshotContactName = template.ContactName,
                    SnapshotContactJobTitle = template.ContactJobTitle,
                    SnapshotContactEmail = template.ContactEmail,
                    SnapshotContactPhone = template.ContactPhone
                };

                var authority = await dbContext.FundingAuthorities.AsNoTracking()
                    .FirstAsync(x => x.Id == first.FundingAuthorityId && x.TenantId == tenantId, cancellationToken);
                var category = await dbContext.InvoiceCategories.AsNoTracking()
                    .FirstAsync(x => x.Id == first.InvoiceCategoryId && x.TenantId == tenantId, cancellationToken);
                var careHome = await dbContext.CareHomes.AsNoTracking()
                    .FirstAsync(x => x.Id == first.CareHomeId && x.TenantId == tenantId, cancellationToken);

                invoice.SnapshotFundingAuthorityCode = authority.Code;
                invoice.SnapshotInvoiceCategoryCode = category.Code;
                invoice.SnapshotCareHomeCode = careHome.Code;
                invoice.RecipientEmail ??= authority.Email;

                foreach (var line in group)
                {
                    invoice.Lines.Add(new InvoiceLine
                    {
                        ClientId = line.ClientId,
                        ClientFundingContractId = line.ClientFundingContractId,
                        FundingRateId = line.FundingRateId,
                        MiscChargeId = line.MiscChargeId,
                        SnapshotClientReferenceNumber = line.ClientReference,
                        SnapshotSageId = line.SageId,
                        SnapshotClientName = line.ClientName,
                        SnapshotCareHomeName = line.CareHomeName,
                        SnapshotCompanyName = line.CompanyName,
                        SnapshotFundingAuthorityCode = invoice.SnapshotFundingAuthorityCode,
                        SnapshotFundingAuthorityName = line.FundingAuthorityName,
                        SnapshotInvoiceCategoryCode = invoice.SnapshotInvoiceCategoryCode,
                        SnapshotInvoiceCategoryName = line.InvoiceCategoryName,
                        SnapshotNominalCode = line.NominalCode,
                        SnapshotNominalCodeName = line.NominalCodeName ?? line.NominalCode,
                        ServicePeriodStart = line.ServiceFrom,
                        ServicePeriodEnd = line.ServiceTo,
                        RateFrequency = line.Frequency,
                        RateAmount = line.Rate,
                        EligibleDays = line.EligibleDays,
                        LineAmount = line.Amount,
                        Description = line.Description,
                        AmountBasis = InvoiceLineAmountBasis.Format(
                            line.EligibleDays,
                            line.Frequency,
                            line.Rate,
                            line.MiscChargeId is not null)
                    });

                    if (line.MiscChargeId is int miscId)
                    {
                        var charge = await dbContext.MiscCharges
                            .FirstAsync(x => x.Id == miscId && x.TenantId == tenantId, cancellationToken);
                        charge.IsInvoiced = true;
                    }
                }

                invoice.TotalAmount = Money.Round(invoice.Lines.Sum(x => x.LineAmount));
                dbContext.Invoices.Add(invoice);
                await dbContext.SaveChangesAsync(cancellationToken);
                createdIds.Add(invoice.Id);
            }

            await audit.LogAsync(
                "Invoice",
                string.Join(",", createdIds),
                "Generate",
                null,
                new { InvoiceIds = createdIds, request.PeriodStart, request.PeriodEnd },
                $"Generated {createdIds.Count} invoice(s).",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Billing generate completed. TenantId={TenantId} InvoiceCount={InvoiceCount} InvoiceIds={InvoiceIds}",
                tenantId,
                createdIds.Count,
                string.Join(",", createdIds));

            return (new BillingGenerateResponse
            {
                InvoiceIds = createdIds,
                InvoiceCount = createdIds.Count,
                TotalAmount = Money.Round(lines.Sum(x => x.Amount)),
                Exceptions = exceptions
            }, null);
        }

        private async Task<(List<BillingPreviewLineDto> Lines, List<BillingExceptionDto> Exceptions, List<BillingCoverageDto> Coverage)> BuildPreviewAsync(
            int tenantId,
            BillingPreviewRequest request,
            CancellationToken cancellationToken)
        {
            var lines = new List<BillingPreviewLineDto>();
            var exceptions = new List<BillingExceptionDto>();
            var coverage = new List<BillingCoverageDto>();

            if (request.PeriodEnd < request.PeriodStart)
            {
                exceptions.Add(Error("INVALID_PERIOD", "Billing period end cannot be before start."));
                return (lines, exceptions, coverage);
            }

            var company = await dbContext.Companies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.CompanyId && x.TenantId == tenantId, cancellationToken);
            if (company is null)
            {
                exceptions.Add(Error("INVALID_COMPANY", "Selected company was not found in this organisation."));
                return (lines, exceptions, coverage);
            }

            var settings = await dbContext.TenantSettings.AsNoTracking()
                .FirstAsync(x => x.TenantId == tenantId, cancellationToken);
            var currency = settings.CurrencySymbol;

            var allowedHomes = await userAccess.GetAllowedCareHomeIdsAsync(cancellationToken);

            var clientsQuery = dbContext.Clients
                .AsNoTracking()
                .Include(x => x.CareHome)
                    .ThenInclude(x => x.Company)
                .Include(x => x.FundingContracts)
                    .ThenInclude(x => x.Rates)
                .Include(x => x.FundingContracts)
                    .ThenInclude(x => x.FundingAuthority)
                .Include(x => x.FundingContracts)
                    .ThenInclude(x => x.InvoiceCategory)
                .Include(x => x.FundingContracts)
                    .ThenInclude(x => x.NominalCode)
                .Where(x => x.TenantId == tenantId)
                .Where(x => !x.IsArchived)
                .Where(x => x.CareHome.CompanyId == request.CompanyId);

            if (request.CareHomeId.HasValue)
            {
                clientsQuery = clientsQuery.Where(x => x.CareHomeId == request.CareHomeId.Value);
            }

            if (allowedHomes is not null)
            {
                clientsQuery = clientsQuery.Where(x => allowedHomes.Contains(x.CareHomeId));
            }

            if (request.ClientIds is { Count: > 0 })
            {
                clientsQuery = clientsQuery.Where(x => request.ClientIds.Contains(x.Id));
            }

            var clients = await clientsQuery.ToListAsync(cancellationToken);

            foreach (var client in clients)
            {
                var occupancy = DateRanges.Intersect(
                    client.AdmissionDate,
                    client.DischargeDate,
                    request.PeriodStart,
                    request.PeriodEnd);

                if (occupancy is null)
                {
                    continue;
                }

                var contracts = client.FundingContracts
                    .Where(c => c.Status == FundingContractStatuses.Active)
                    .Where(c => !request.InvoiceCategoryId.HasValue || c.InvoiceCategoryId == request.InvoiceCategoryId.Value)
                    .ToList();

                if (contracts.Count == 0)
                {
                    exceptions.Add(Error(
                        "MISSING_CONTRACT",
                        $"No active funding contract covers {client.FirstName} {client.LastName} for this period.",
                        client));

                    continue;
                }

                var applicable = contracts
                    .Where(c => DateRanges.Intersect(
                        occupancy.Value.Start,
                        occupancy.Value.End,
                        c.ContractStartDate,
                        c.ContractEndDate) is not null)
                    .ToList();

                var blockedStreams = new HashSet<(int FundingAuthorityId, int InvoiceCategoryId)>();
                foreach (var stream in applicable.GroupBy(c => (c.FundingAuthorityId, c.InvoiceCategoryId)))
                {
                    var streamContracts = stream.ToList();
                    var pairs = FundingContractOverlap.FindOverlappingPairs(streamContracts);
                    if (pairs.Count == 0)
                    {
                        continue;
                    }

                    blockedStreams.Add(stream.Key);
                    var (left, right) = pairs[0];
                    var contractOverlap = DateRanges.Intersect(
                        left.ContractStartDate,
                        left.ContractEndDate,
                        right.ContractStartDate,
                        right.ContractEndDate);
                    var billedOverlap = contractOverlap is null
                        ? null
                        : DateRanges.Intersect(
                            occupancy.Value.Start,
                            occupancy.Value.End,
                            contractOverlap.Value.Start,
                            contractOverlap.Value.End);
                    var overlapStart = billedOverlap?.Start ?? contractOverlap?.Start;
                    var overlapEnd = billedOverlap?.End ?? contractOverlap?.End;
                    var ids = streamContracts.Select(c => c.Id).Distinct().OrderBy(id => id).ToList();
                    var authorityName = streamContracts[0].FundingAuthority.Name;
                    var categoryName = streamContracts[0].InvoiceCategory.Name;
                    var overlapText = overlapStart is null
                        ? "unknown"
                        : $"{overlapStart:yyyy-MM-dd} to {FundingContractOverlap.FormatOpenEnded(overlapEnd)}";

                    exceptions.Add(new BillingExceptionDto
                    {
                        Severity = "Error",
                        Code = FundingContractOverlap.BillingCode,
                        Message =
                            $"Overlapping funding contracts for {FormatName(client)} / {authorityName} / {categoryName}. " +
                            $"Contract IDs: {string.Join(", ", ids)}. Overlapping dates: {overlapText}. " +
                            "Billing is blocked until the overlap is resolved.",
                        ClientId = client.Id,
                        ClientName = FormatName(client),
                        CareHomeId = client.CareHomeId,
                        ContractIds = ids,
                        FundingAuthorityName = authorityName,
                        InvoiceCategoryName = categoryName,
                        OverlapStart = overlapStart,
                        OverlapEnd = overlapEnd == DateRanges.OpenEnded ? null : overlapEnd
                    });
                }

                foreach (var contract in contracts)
                {
                    if (blockedStreams.Contains((contract.FundingAuthorityId, contract.InvoiceCategoryId)))
                    {
                        continue;
                    }

                    var contractSlice = DateRanges.Intersect(
                        occupancy.Value.Start,
                        occupancy.Value.End,
                        contract.ContractStartDate,
                        contract.ContractEndDate);

                    if (contractSlice is null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(contract.NominalCode.Code))
                    {
                        exceptions.Add(Error(
                            "MISSING_NOMINAL",
                            $"Nominal code is missing on the funding contract for {client.FirstName} {client.LastName}.",
                            client,
                            contract.Id));
                        continue;
                    }

                    var template = await templateResolver.ResolveAsync(
                        tenantId,
                        contract.InvoiceCategoryId,
                        contract.FundingAuthorityId,
                        client.CareHomeId,
                        client.CareHome.CompanyId,
                        cancellationToken);

                    if (template is null)
                    {
                        exceptions.Add(Error(
                            "MISSING_TEMPLATE",
                            $"No invoice template found for {contract.InvoiceCategory.Name} / {contract.FundingAuthority.Name} / {client.CareHome.Name}.",
                            client,
                            contract.Id));
                    }

                    var billed = await GetFinalizedPeriodsAsync(
                        tenantId,
                        client.Id,
                        contract.Id,
                        contract.InvoiceCategoryId,
                        cancellationToken);

                    var remaining = DateRanges.Subtract(
                        contractSlice.Value.Start,
                        contractSlice.Value.End,
                        billed);

                    var alreadyBilledPeriods = billed
                        .Select(b => DateRanges.Intersect(
                            b.Start,
                            b.End,
                            contractSlice.Value.Start,
                            contractSlice.Value.End))
                        .Where(x => x.HasValue)
                        .Select(x => ToRange(x!.Value.Start, x.Value.End))
                        .ToList();
                    var remainingPeriods = remaining.Select(r => ToRange(r.Start, r.End)).ToList();
                    var skippedDays = alreadyBilledPeriods.Sum(x => x.Days);

                    coverage.Add(new BillingCoverageDto
                    {
                        ClientId = client.Id,
                        ClientName = FormatName(client),
                        ClientFundingContractId = contract.Id,
                        RequestedPeriodStart = request.PeriodStart,
                        RequestedPeriodEnd = request.PeriodEnd,
                        AlreadyBilledPeriods = alreadyBilledPeriods,
                        RemainingBillablePeriods = remainingPeriods,
                        SkippedAlreadyBilledDays = skippedDays
                    });

                    if (remaining.Count == 0)
                    {
                        if (skippedDays > 0)
                        {
                            exceptions.Add(Warning(
                                "ALREADY_FULLY_BILLED",
                                $"The requested period is already fully billed for {FormatName(client)}. No unbilled service dates remain.",
                                client,
                                contract.Id));
                        }

                        continue;
                    }

                    if (skippedDays > 0)
                    {
                        exceptions.Add(Info(
                            "PARTIAL_PERIOD_BILLING",
                            $"Requested period overlaps already-finalized invoices for {FormatName(client)}. Only previously unbilled dates will be invoiced.",
                            client,
                            contract.Id));
                    }

                    foreach (var fragment in remaining)
                    {
                        var rates = contract.Rates
                            .OrderBy(r => r.EffectiveFrom)
                            .ToList();

                        if (rates.Count == 0)
                        {
                            exceptions.Add(Error(
                                "MISSING_RATE",
                                $"No funding rate exists for {client.FirstName} {client.LastName} ({contract.FundingAuthority.Name}). Billing will not assume {currency}0.",
                                client,
                                contract.Id));
                            continue;
                        }

                        var covered = false;

                        foreach (var rate in rates)
                        {
                            var rateSlice = DateRanges.Intersect(
                                fragment.Start,
                                fragment.End,
                                rate.EffectiveFrom,
                                rate.EffectiveTo);

                            if (rateSlice is null)
                            {
                                continue;
                            }

                            covered = true;
                            var days = DateRanges.InclusiveDays(rateSlice.Value.Start, rateSlice.Value.End);
                            var amount = rateCalculator.Calculate(
                                rate.Frequency,
                                rate.Amount,
                                rateSlice.Value.Start,
                                rateSlice.Value.End);

                            lines.Add(new BillingPreviewLineDto
                            {
                                ClientId = client.Id,
                                ClientName = FormatName(client),
                                ClientReference = client.ReferenceNumber,
                                SageId = client.SageId,
                                CareHomeId = client.CareHomeId,
                                CareHomeName = client.CareHome.Name,
                                CompanyId = client.CareHome.CompanyId,
                                CompanyName = client.CareHome.Company.Name,
                                FundingAuthorityId = contract.FundingAuthorityId,
                                FundingAuthorityName = contract.FundingAuthority.Name,
                                InvoiceCategoryId = contract.InvoiceCategoryId,
                                InvoiceCategoryName = contract.InvoiceCategory.Name,
                                NominalCodeId = contract.NominalCodeId,
                                NominalCode = contract.NominalCode.Code,
                                NominalCodeName = contract.NominalCode.Name,
                                ClientFundingContractId = contract.Id,
                                FundingRateId = rate.Id,
                                ServiceFrom = rateSlice.Value.Start,
                                ServiceTo = rateSlice.Value.End,
                                EligibleDays = days,
                                Frequency = rate.Frequency,
                                Rate = rate.Amount,
                                Amount = amount,
                                Description = $"{contract.InvoiceCategory.Name} {rateSlice.Value.Start:yyyy-MM-dd} to {rateSlice.Value.End:yyyy-MM-dd}",
                                InvoiceTemplateId = template?.Id
                            });
                        }

                        if (!covered)
                        {
                            exceptions.Add(Error(
                                "MISSING_RATE",
                                $"No applicable rate covers {fragment.Start:yyyy-MM-dd} to {fragment.End:yyyy-MM-dd} for {client.FirstName} {client.LastName}. Billing will not assume {currency}0.",
                                client,
                                contract.Id));
                        }
                    }
                }
            }

            var miscCategory = await dbContext.InvoiceCategories.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.Code == DefaultInvoiceCategories.MiscellaneousCode,
                    cancellationToken);

            if (!request.InvoiceCategoryId.HasValue || request.InvoiceCategoryId == miscCategory?.Id)
            {
                await AddUnbilledMiscChargesAsync(tenantId, request, allowedHomes, lines, exceptions, cancellationToken);
            }

            if (lines.Count == 0 && coverage.Any(x => x.SkippedAlreadyBilledDays > 0))
            {
                exceptions.RemoveAll(x => x.Code == "ALREADY_FULLY_BILLED");
                exceptions.Add(Error(
                    "ALREADY_FULLY_BILLED",
                    "The requested period is already fully billed. No unbilled service dates remain."));
            }

            return (lines, exceptions, coverage);
        }

        private async Task AddUnbilledMiscChargesAsync(
            int tenantId,
            BillingPreviewRequest request,
            List<int>? allowedHomes,
            List<BillingPreviewLineDto> lines,
            List<BillingExceptionDto> exceptions,
            CancellationToken cancellationToken)
        {
            var query = dbContext.MiscCharges
                .AsNoTracking()
                .Include(x => x.Client)
                    .ThenInclude(c => c.CareHome)
                        .ThenInclude(h => h.Company)
                .Include(x => x.Client)
                    .ThenInclude(c => c.FundingContracts)
                        .ThenInclude(c => c.FundingAuthority)
                .Include(x => x.NominalCode)
                .Where(x => x.TenantId == tenantId)
                .Where(x => !x.IsInvoiced)
                .Where(x => x.UsedDate >= request.PeriodStart && x.UsedDate <= request.PeriodEnd)
                .Where(x => x.Client.CareHome.CompanyId == request.CompanyId)
                .Where(x => !x.Client.IsArchived);

            if (request.CareHomeId.HasValue)
            {
                query = query.Where(x => x.Client.CareHomeId == request.CareHomeId.Value);
            }

            if (allowedHomes is not null)
            {
                query = query.Where(x => allowedHomes.Contains(x.Client.CareHomeId));
            }

            if (request.ClientIds is { Count: > 0 })
            {
                query = query.Where(x => request.ClientIds.Contains(x.ClientId));
            }

            var charges = await query.ToListAsync(cancellationToken);
            var miscCategory = await dbContext.InvoiceCategories.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenantId && x.Code == DefaultInvoiceCategories.MiscellaneousCode,
                    cancellationToken);

            if (miscCategory is null && charges.Count > 0)
            {
                exceptions.Add(Error("MISSING_CATEGORY", "MISC invoice category is missing from master data."));
                return;
            }

            foreach (var charge in charges)
            {
                var nominal = charge.NominalCode?.Code ?? charge.NominalCodeValue;
                if (string.IsNullOrWhiteSpace(nominal))
                {
                    exceptions.Add(Error(
                        "MISSING_NOMINAL",
                        $"Miscellaneous charge on {charge.UsedDate:yyyy-MM-dd} for {charge.Client.FirstName} {charge.Client.LastName} has no nominal code.",
                        charge.Client));
                    continue;
                }

                var template = await templateResolver.ResolveAsync(
                    tenantId,
                    miscCategory!.Id,
                    0,
                    charge.Client.CareHomeId,
                    charge.Client.CareHome.CompanyId,
                    cancellationToken);

                // Misc charges need a funding authority on the invoice header. Use the client's first active contract authority if present.
                var authority = charge.Client.FundingContracts
                    .FirstOrDefault(c => c.Status == FundingContractStatuses.Active)?.FundingAuthority;

                if (authority is null)
                {
                    authority = await dbContext.FundingAuthorities.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);
                }

                if (authority is null)
                {
                    exceptions.Add(Error(
                        "MISSING_CONTRACT",
                        $"Cannot invoice miscellaneous charge for {charge.Client.FirstName} {charge.Client.LastName} without a funding authority.",
                        charge.Client));
                    continue;
                }

                if (template is null)
                {
                    template = await templateResolver.ResolveAsync(
                        tenantId,
                        miscCategory.Id,
                        authority.Id,
                        charge.Client.CareHomeId,
                        charge.Client.CareHome.CompanyId,
                        cancellationToken);
                }

                if (template is null)
                {
                    exceptions.Add(Error(
                        "MISSING_TEMPLATE",
                        $"No invoice template found for miscellaneous charges ({charge.Client.CareHome.Name}).",
                        charge.Client));
                }

                var contract = charge.Client.FundingContracts.FirstOrDefault(c => c.Status == FundingContractStatuses.Active)
                    ?? charge.Client.FundingContracts.FirstOrDefault();

                if (contract is null)
                {
                    exceptions.Add(Error(
                        "MISSING_CONTRACT",
                        $"Client {charge.Client.FirstName} {charge.Client.LastName} has a miscellaneous charge but no funding contract to hang the invoice line on.",
                        charge.Client));
                    continue;
                }

                lines.Add(new BillingPreviewLineDto
                {
                    ClientId = charge.ClientId,
                    ClientName = FormatName(charge.Client),
                    ClientReference = charge.Client.ReferenceNumber,
                    SageId = charge.Client.SageId,
                    CareHomeId = charge.Client.CareHomeId,
                    CareHomeName = charge.Client.CareHome.Name,
                    CompanyId = charge.Client.CareHome.CompanyId,
                    CompanyName = charge.Client.CareHome.Company.Name,
                    FundingAuthorityId = authority.Id,
                    FundingAuthorityName = authority.Name,
                    InvoiceCategoryId = miscCategory.Id,
                    InvoiceCategoryName = miscCategory.Name,
                    NominalCodeId = charge.NominalCodeId ?? contract.NominalCodeId,
                    NominalCode = nominal,
                    ClientFundingContractId = contract.Id,
                    MiscChargeId = charge.Id,
                    ServiceFrom = charge.UsedDate,
                    ServiceTo = charge.UsedDate,
                    EligibleDays = 1,
                    Frequency = "AdHoc",
                    Rate = charge.Amount,
                    Amount = Money.Round(charge.Amount),
                    Description = charge.Description,
                    InvoiceTemplateId = template?.Id
                });
            }
        }

        private async Task<List<(DateOnly Start, DateOnly End)>> GetFinalizedPeriodsAsync(
            int tenantId,
            int clientId,
            int contractId,
            int invoiceCategoryId,
            CancellationToken cancellationToken)
        {
            var billed = await dbContext.InvoiceLines
                .AsNoTracking()
                .Where(x =>
                    x.Invoice.TenantId == tenantId &&
                    x.ClientId == clientId &&
                    x.ClientFundingContractId == contractId &&
                    x.Invoice.InvoiceCategoryId == invoiceCategoryId &&
                    x.Invoice.Status != InvoiceStatuses.Void)
                .Select(x => new { x.ServicePeriodStart, x.ServicePeriodEnd })
                .ToListAsync(cancellationToken);

            return billed.Select(x => (x.ServicePeriodStart, x.ServicePeriodEnd)).ToList();
        }

        private async Task<bool> HasFinalizedOverlapAsync(
            int tenantId,
            int clientId,
            int contractId,
            int invoiceCategoryId,
            DateOnly start,
            DateOnly end,
            CancellationToken cancellationToken)
        {
            return await dbContext.InvoiceLines.AnyAsync(x =>
                x.Invoice.TenantId == tenantId &&
                x.ClientId == clientId &&
                x.ClientFundingContractId == contractId &&
                x.Invoice.InvoiceCategoryId == invoiceCategoryId &&
                x.Invoice.Status != InvoiceStatuses.Void &&
                x.ServicePeriodStart <= end &&
                x.ServicePeriodEnd >= start,
                cancellationToken);
        }

        private Task AcquireBillingLockAsync(int tenantId, CancellationToken cancellationToken) =>
            SqlAppLock.AcquireExclusiveAsync(
                dbContext.Database,
                $"billing-generate-{tenantId}",
                cancellationToken);

        private static BillingDateRangeDto ToRange(DateOnly start, DateOnly end)
        {
            return new BillingDateRangeDto
            {
                Start = start,
                End = end,
                Days = DateRanges.InclusiveDays(start, end)
            };
        }

        private static BillingExceptionDto Error(
            string code,
            string message,
            Client? client = null,
            int? contractId = null)
        {
            return Exception("Error", code, message, client, contractId);
        }

        private static BillingExceptionDto Warning(
            string code,
            string message,
            Client? client = null,
            int? contractId = null)
        {
            return Exception("Warning", code, message, client, contractId);
        }

        private static BillingExceptionDto Info(
            string code,
            string message,
            Client? client = null,
            int? contractId = null)
        {
            return Exception("Info", code, message, client, contractId);
        }

        private static BillingExceptionDto Exception(
            string severity,
            string code,
            string message,
            Client? client,
            int? contractId)
        {
            return new BillingExceptionDto
            {
                Severity = severity,
                Code = code,
                Message = message,
                ClientId = client?.Id,
                ClientName = client is null ? null : FormatName(client),
                CareHomeId = client?.CareHomeId,
                ClientFundingContractId = contractId
            };
        }

        private static string FormatName(Client client)
        {
            return string.Join(" ", new[] { client.FirstName, client.LastName }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}

