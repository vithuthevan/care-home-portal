using CareHome.Api.Models;
using CareHome.Api.Security;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CareHome.Api.Data
{
    public class CareHomeDbContext : IdentityDbContext<ApplicationUser>
    {
        public CareHomeDbContext(
            DbContextOptions<CareHomeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CareHomeLocation> CareHomes => Set<CareHomeLocation>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<ClientGuardian> ClientGuardians => Set<ClientGuardian>();
        public DbSet<FundingAuthority> FundingAuthorities => Set<FundingAuthority>();
        public DbSet<InvoiceCategory> InvoiceCategories => Set<InvoiceCategory>();
        public DbSet<NominalCode> NominalCodes => Set<NominalCode>();
        public DbSet<ClientFundingContract> ClientFundingContracts => Set<ClientFundingContract>();
        public DbSet<FundingRate> FundingRates => Set<FundingRate>();
        public DbSet<InvoiceTemplate> InvoiceTemplates => Set<InvoiceTemplate>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
        public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
        public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();
        public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();
        public DbSet<MiscChargeImportBatch> MiscChargeImportBatches => Set<MiscChargeImportBatch>();
        public DbSet<MiscCharge> MiscCharges => Set<MiscCharge>();
        public DbSet<SageExportBatch> SageExportBatches => Set<SageExportBatch>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<BillingExceptionLog> BillingExceptionLogs => Set<BillingExceptionLog>();
        public DbSet<EmailSendLog> EmailSendLogs => Set<EmailSendLog>();
        public DbSet<UserCareHomeAccess> UserCareHomeAccess => Set<UserCareHomeAccess>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
        public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
        public DbSet<BankImportBatch> BankImportBatches => Set<BankImportBatch>();
        public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
        public DbSet<ReconciliationMatchGroup> ReconciliationMatchGroups => Set<ReconciliationMatchGroup>();
        public DbSet<ReconciliationMatchGroupLine> ReconciliationMatchGroupLines => Set<ReconciliationMatchGroupLine>();
        public DbSet<ReconciliationSuggestion> ReconciliationSuggestions => Set<ReconciliationSuggestion>();
        public DbSet<ReconciliationSuggestionLine> ReconciliationSuggestionLines => Set<ReconciliationSuggestionLine>();
        public DbSet<PaymentReconciliation> PaymentReconciliations => Set<PaymentReconciliation>();
        public DbSet<BankCsvMappingTemplate> BankCsvMappingTemplates => Set<BankCsvMappingTemplate>();
        public DbSet<RemittanceBatch> RemittanceBatches => Set<RemittanceBatch>();
        public DbSet<RemittanceLine> RemittanceLines => Set<RemittanceLine>();
        public DbSet<RevenueAssuranceFinding> RevenueAssuranceFindings => Set<RevenueAssuranceFinding>();
        public DbSet<InvoiceDispute> InvoiceDisputes => Set<InvoiceDispute>();
        public DbSet<DisputeMessage> DisputeMessages => Set<DisputeMessage>();
        public DbSet<FundingContractRenewal> FundingContractRenewals => Set<FundingContractRenewal>();
        public DbSet<CollectionPolicy> CollectionPolicies => Set<CollectionPolicy>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.DisplayName)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired(false);

                entity.HasIndex(x => x.TenantId);
            });

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(x => x.PublicId)
                    .IsUnique();

                entity.HasIndex(x => x.Name);
            });

            modelBuilder.Entity<TenantSettings>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId)
                    .IsUnique();

                entity.HasOne(x => x.Tenant)
                    .WithOne(x => x.Settings)
                    .HasForeignKey<TenantSettings>(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.TenantId, x.PublicId })
                    .IsUnique();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(x => new { x.TenantId, x.Name })
                    .IsUnique();

                entity.HasIndex(x => new { x.TenantId, x.IsActive });

                entity.HasOne(x => x.Tenant)
                    .WithMany(x => x.Companies)
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CareHomeLocation>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.TenantId, x.PublicId })
                    .IsUnique();

                entity.Property(x => x.PortalAccentTheme)
                    .HasMaxLength(20);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.LogoPath)
                    .HasMaxLength(500);

                entity.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique();

                entity.HasIndex(x => new { x.TenantId, x.IsActive });

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Company)
                    .WithMany(x => x.CareHomes)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.TenantId, x.PublicId })
                    .IsUnique();

                entity.Property(x => x.SageId)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(x => x.ReferenceNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(x => x.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.CareType)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.DateOfBirth)
                    .HasColumnType("date");

                entity.Property(x => x.AdmissionDate)
                    .HasColumnType("date");

                entity.Property(x => x.DischargeDate)
                    .HasColumnType("date");

                entity.HasIndex(x => new { x.TenantId, x.SageId })
                    .IsUnique();

                entity.HasIndex(x => new { x.TenantId, x.ReferenceNumber })
                    .IsUnique();

                entity.HasIndex(x => x.TenantId);

                entity.HasIndex(x => x.CareHomeId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CareHome)
                    .WithMany(x => x.Clients)
                    .HasForeignKey(x => x.CareHomeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Guardian)
                    .WithOne(x => x.Client)
                    .HasForeignKey<ClientGuardian>(x => x.ClientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ClientGuardian>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(x => x.ClientId)
                    .IsUnique();

                entity.HasIndex(x => x.TenantId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FundingAuthority>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.PublicId)
                    .IsRequired();

                entity.HasIndex(x => new { x.TenantId, x.PublicId })
                    .IsUnique();

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Type)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.BillingFrequency)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.CycleAnchorDate)
                    .HasColumnType("date");

                entity.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique();

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InvoiceCategory>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.GroupingMode)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique();

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<NominalCode>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(x => new { x.TenantId, x.Code })
                    .IsUnique();

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ClientFundingContract>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.ContractStartDate)
                    .HasColumnType("date");

                entity.Property(x => x.ContractEndDate)
                    .HasColumnType("date");

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.ClientId);
                entity.HasIndex(x => x.FundingAuthorityId);
                entity.HasIndex(x => x.InvoiceCategoryId);
                entity.HasIndex(x => x.NominalCodeId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Client)
                    .WithMany(x => x.FundingContracts)
                    .HasForeignKey(x => x.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.FundingAuthority)
                    .WithMany(x => x.FundingContracts)
                    .HasForeignKey(x => x.FundingAuthorityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InvoiceCategory)
                    .WithMany(x => x.FundingContracts)
                    .HasForeignKey(x => x.InvoiceCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.NominalCode)
                    .WithMany(x => x.FundingContracts)
                    .HasForeignKey(x => x.NominalCodeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InvoiceTemplate)
                    .WithMany()
                    .HasForeignKey(x => x.InvoiceTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<FundingRate>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Frequency)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.EffectiveFrom)
                    .HasColumnType("date");

                entity.Property(x => x.EffectiveTo)
                    .HasColumnType("date");

                entity.HasIndex(x => x.ClientFundingContractId);

                entity.HasOne(x => x.ClientFundingContract)
                    .WithMany(x => x.Rates)
                    .HasForeignKey(x => x.ClientFundingContractId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InvoiceTemplate>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.InvoiceCategoryId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InvoiceCategory)
                    .WithMany()
                    .HasForeignKey(x => x.InvoiceCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.FundingAuthority)
                    .WithMany()
                    .HasForeignKey(x => x.FundingAuthorityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CareHome)
                    .WithMany()
                    .HasForeignKey(x => x.CareHomeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Company)
                    .WithMany(x => x.InvoiceTemplates)
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DocumentSequence>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.DocumentType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => new { x.TenantId, x.DocumentType })
                    .IsUnique();

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.TenantId, x.PublicId })
                    .IsUnique();

                entity.Property(x => x.InvoiceNumber)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.HasIndex(x => new { x.TenantId, x.InvoiceNumber })
                    .IsUnique();

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.InvoiceDate)
                    .HasColumnType("date");

                entity.Property(x => x.DueDate)
                    .HasColumnType("date");

                entity.Property(x => x.PeriodStart)
                    .HasColumnType("date");

                entity.Property(x => x.PeriodEnd)
                    .HasColumnType("date");

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.CareHomeId);
                entity.HasIndex(x => x.FundingAuthorityId);
                entity.HasIndex(x => x.InvoiceCategoryId);
                entity.HasIndex(x => new { x.InvoiceDate, x.Status });

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CareHome)
                    .WithMany()
                    .HasForeignKey(x => x.CareHomeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.FundingAuthority)
                    .WithMany()
                    .HasForeignKey(x => x.FundingAuthorityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InvoiceCategory)
                    .WithMany()
                    .HasForeignKey(x => x.InvoiceCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InvoiceTemplate)
                    .WithMany()
                    .HasForeignKey(x => x.InvoiceTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.SageExportBatch)
                    .WithMany(x => x.Invoices)
                    .HasForeignKey(x => x.SageExportBatchId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InvoiceLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.RateAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.LineAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.AmountBasis)
                    .HasMaxLength(500);

                entity.Property(x => x.ServicePeriodStart)
                    .HasColumnType("date");

                entity.Property(x => x.ServicePeriodEnd)
                    .HasColumnType("date");

                entity.HasIndex(x => x.InvoiceId);
                entity.HasIndex(x => new { x.ClientId, x.ClientFundingContractId, x.ServicePeriodStart, x.ServicePeriodEnd });

                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Client)
                    .WithMany()
                    .HasForeignKey(x => x.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ClientFundingContract)
                    .WithMany()
                    .HasForeignKey(x => x.ClientFundingContractId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.FundingRate)
                    .WithMany()
                    .HasForeignKey(x => x.FundingRateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.MiscCharge)
                    .WithMany()
                    .HasForeignKey(x => x.MiscChargeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CreditNote>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.CreditNoteNumber)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.HasIndex(x => new { x.TenantId, x.CreditNoteNumber })
                    .IsUnique();

                entity.Property(x => x.TotalAmount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.CreditNoteDate)
                    .HasColumnType("date");

                entity.Property(x => x.PeriodStart)
                    .HasColumnType("date");

                entity.Property(x => x.PeriodEnd)
                    .HasColumnType("date");

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.InvoiceId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Invoice)
                    .WithMany(x => x.CreditNotes)
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CreditNoteLine>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ServicePeriodStart)
                    .HasColumnType("date");

                entity.Property(x => x.ServicePeriodEnd)
                    .HasColumnType("date");

                entity.HasOne(x => x.CreditNote)
                    .WithMany(x => x.Lines)
                    .HasForeignKey(x => x.CreditNoteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InvoiceLine)
                    .WithMany(x => x.CreditNoteLines)
                    .HasForeignKey(x => x.InvoiceLineId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MiscChargeImportBatch>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<MiscCharge>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.UsedDate)
                    .HasColumnType("date");

                entity.HasIndex(x => new { x.TenantId, x.ClientId, x.UsedDate, x.Description, x.Amount })
                    .IsUnique();

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.ClientId);
                entity.HasIndex(x => x.ImportBatchId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ImportBatch)
                    .WithMany(x => x.Charges)
                    .HasForeignKey(x => x.ImportBatchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Client)
                    .WithMany()
                    .HasForeignKey(x => x.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.NominalCode)
                    .WithMany()
                    .HasForeignKey(x => x.NominalCodeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SageExportBatch>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.DateFrom)
                    .HasColumnType("date");

                entity.Property(x => x.DateTo)
                    .HasColumnType("date");

                entity.HasIndex(x => x.TenantId);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.LoggedAt);
                entity.HasIndex(x => new { x.EntityType, x.EntityId });

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BillingExceptionLog>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.PeriodStart)
                    .HasColumnType("date");

                entity.Property(x => x.PeriodEnd)
                    .HasColumnType("date");

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.LoggedAt);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Client)
                    .WithMany()
                    .HasForeignKey(x => x.ClientId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CareHome)
                    .WithMany()
                    .HasForeignKey(x => x.CareHomeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EmailSendLog>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.TenantId);
                entity.HasIndex(x => x.AttemptedAt);

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserCareHomeAccess>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.UserId, x.CareHomeId })
                    .IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.CareHomeAccess)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.CareHome)
                    .WithMany(x => x.UserAccess)
                    .HasForeignKey(x => x.CareHomeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.TenantId, x.PublicId })
                    .IsUnique();

                entity.HasIndex(x => new { x.TenantId, x.ReceivedDate });
                entity.HasIndex(x => new { x.TenantId, x.Reference });

                entity.HasIndex(x => new { x.TenantId, x.Source, x.ExternalReference })
                    .IsUnique()
                    .HasFilter("[ExternalReference] IS NOT NULL");

                entity.Property(x => x.Amount)
                    .HasPrecision(18, 2);

                entity.Property(x => x.ReceivedDate)
                    .HasColumnType("date");

                entity.Property(x => x.RowVersion)
                    .IsRowVersion();

                entity.HasCheckConstraint("CK_Payments_Amount_Positive", "[Amount] > 0");

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.FundingAuthority)
                    .WithMany()
                    .HasForeignKey(x => x.FundingAuthorityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CareHome)
                    .WithMany()
                    .HasForeignKey(x => x.CareHomeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentAllocation>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.PublicId)
                    .IsUnique();

                entity.HasIndex(x => x.PaymentId);
                entity.HasIndex(x => x.InvoiceId);

                entity.Property(x => x.AllocatedAmount)
                    .HasPrecision(18, 2);

                entity.HasCheckConstraint("CK_PaymentAllocations_Amount_Positive", "[AllocatedAmount] > 0");

                entity.HasOne(x => x.Tenant)
                    .WithMany()
                    .HasForeignKey(x => x.TenantId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Payment)
                    .WithMany(x => x.Allocations)
                    .HasForeignKey(x => x.PaymentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Invoice)
                    .WithMany()
                    .HasForeignKey(x => x.InvoiceId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BankAccount>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BankImportBatch>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.BankAccountId, x.ContentChecksum }).IsUnique();
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.BankAccount).WithMany(x => x.ImportBatches).HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BankTransaction>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.BankAccountId, x.RowHash }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.Status });
                entity.Property(x => x.Amount).HasPrecision(18, 2);
                entity.Property(x => x.TransactionDate).HasColumnType("date");
                entity.Property(x => x.ValueDate).HasColumnType("date");
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasCheckConstraint("CK_BankTransactions_Amount_Positive", "[Amount] > 0");
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.BankAccount).WithMany(x => x.Transactions).HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ImportBatch).WithMany(x => x.Transactions).HasForeignKey(x => x.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReconciliationMatchGroup>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReconciliationMatchGroupLine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.AllocatedAmount).HasPrecision(18, 2);
                entity.HasOne(x => x.MatchGroup).WithMany(x => x.Lines).HasForeignKey(x => x.MatchGroupId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReconciliationSuggestion>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.PublicId).IsUnique();
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.BankTransaction).WithMany(x => x.Suggestions).HasForeignKey(x => x.BankTransactionId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ReconciliationSuggestionLine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.SuggestedAmount).HasPrecision(18, 2);
                entity.HasOne(x => x.Suggestion).WithMany(x => x.Lines).HasForeignKey(x => x.SuggestionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PaymentReconciliation>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => x.BankTransactionId).IsUnique().HasFilter("[Status] = 'Confirmed'");
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.BankTransaction).WithOne(x => x.ActiveReconciliation).HasForeignKey<PaymentReconciliation>(x => x.BankTransactionId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.MatchGroup).WithMany().HasForeignKey(x => x.MatchGroupId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BankCsvMappingTemplate>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.Name });
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.BankAccount).WithMany().HasForeignKey(x => x.BankAccountId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RemittanceBatch>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.ContentChecksum }).HasFilter("[ContentChecksum] IS NOT NULL");
                entity.HasIndex(x => new { x.TenantId, x.Status });
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.FundingAuthority).WithMany().HasForeignKey(x => x.FundingAuthorityId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RemittanceLine>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.RemittanceBatchId, x.LineNumber }).IsUnique();
                entity.Property(x => x.GrossAmount).HasPrecision(18, 2);
                entity.Property(x => x.PaidAmount).HasPrecision(18, 2);
                entity.Property(x => x.DeductionAmount).HasPrecision(18, 2);
                entity.HasOne(x => x.Batch).WithMany(x => x.Lines).HasForeignKey(x => x.RemittanceBatchId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.MatchedInvoice).WithMany().HasForeignKey(x => x.MatchedInvoiceId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RevenueAssuranceFinding>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.Status, x.RuleCode });
                entity.Property(x => x.ExpectedValue).HasPrecision(18, 2);
                entity.Property(x => x.ActualValue).HasPrecision(18, 2);
                entity.Property(x => x.EstimatedImpact).HasPrecision(18, 2);
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InvoiceDispute>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.Status });
                entity.Property(x => x.DisputedAmount).HasPrecision(18, 2);
                entity.Property(x => x.OpenedDate).HasColumnType("date");
                entity.Property(x => x.DueDate).HasColumnType("date");
                entity.Property(x => x.ResolvedDate).HasColumnType("date");
                entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Invoice).WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.FundingAuthority).WithMany().HasForeignKey(x => x.FundingAuthorityId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.RemittanceLine).WithMany().HasForeignKey(x => x.RemittanceLineId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DisputeMessage>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Dispute).WithMany(x => x.Messages).HasForeignKey(x => x.InvoiceDisputeId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FundingContractRenewal>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.Status });
                entity.Property(x => x.CurrentRate).HasPrecision(18, 2);
                entity.Property(x => x.ProposedRate).HasPrecision(18, 2);
                entity.Property(x => x.CurrentEndDate).HasColumnType("date");
                entity.Property(x => x.EffectiveFrom).HasColumnType("date");
                entity.Property(x => x.NextActionDate).HasColumnType("date");
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CollectionPolicy>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TenantId, x.PublicId }).IsUnique();
                entity.HasIndex(x => new { x.TenantId, x.IsDefault });
                entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
