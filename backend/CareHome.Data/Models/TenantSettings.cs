using System.ComponentModel.DataAnnotations;
using CareHome.Api.Common;
using CareHome.Api.Security;

namespace CareHome.Api.Models
{
    public class TenantSettings : ITenantOwned
    {
        public int Id { get; set; }

        public int TenantId { get; set; }

        [Required]
        [MaxLength(3)]
        public string CurrencyCode { get; set; } = "GBP";

        [Required]
        [MaxLength(8)]
        public string CurrencySymbol { get; set; } = "£";

        [Required]
        [MaxLength(80)]
        public string TimeZoneId { get; set; } = "Europe/London";

        [MaxLength(20)]
        public string InvoicePrefix { get; set; } = "INV-";

        [MaxLength(20)]
        public string CreditNotePrefix { get; set; } = "CN-";

        public int NumberLength { get; set; } = 4;

        public int PaymentTermsDays { get; set; } = 30;

        /// <summary>Manual = the user types invoice dates. FunderCycle = periods follow each funder's frequency.</summary>
        [Required]
        [MaxLength(30)]
        public string BillingPeriodMode { get; set; } = BillingPeriodModes.Manual;

        /// <summary>Miscellaneous charges may be invoiced without a funding contract, to a Private payee.</summary>
        public bool AllowPrivatePayer { get; set; }

        /// <summary>Show guardian name and contacts on the resident.</summary>
        public bool ShowGuardian { get; set; }

        /// <summary>Banking, remittances, collections, and disputes for this organisation.</summary>
        public bool FinanceModuleEnabled { get; set; }

        [MaxLength(150)]
        public string? EmailFromName { get; set; }

        [MaxLength(150)]
        public string? EmailFromAddress { get; set; }

        [MaxLength(20)]
        public string? PrimaryColour { get; set; }

        public Tenant Tenant { get; set; } = null!;
    }
}

