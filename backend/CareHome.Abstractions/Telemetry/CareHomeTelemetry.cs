using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace CareHome.Api.Telemetry;

/// <summary>
/// Shared metric definitions referenced by domain modules and the API host.
/// </summary>
public static class CareHomeTelemetry
{
    public const string ActivitySourceName = "CareHome.Api";
    public const string MeterName = "CareHome.Api";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> BillingGenerateAttempts =
        Meter.CreateCounter<long>("carehome.billing.generate.attempts", description: "Billing generate API attempts");

    public static readonly Counter<long> BillingGenerateFailures =
        Meter.CreateCounter<long>("carehome.billing.generate.failures", description: "Billing generate blocked or failed");

    public static readonly Counter<long> EmailSendFailures =
        Meter.CreateCounter<long>("carehome.email.send.failures", description: "Outbound email send failures");

    public static readonly Counter<long> ReceivablesOutstandingQuery =
        Meter.CreateCounter<long>("carehome.receivables.outstanding_query", description: "Receivables outstanding queries");

    public static readonly Counter<long> ReceivablesAgeingQuery =
        Meter.CreateCounter<long>("carehome.receivables.ageing_query", description: "Receivables ageing queries");

    public static readonly Counter<long> PaymentCreated =
        Meter.CreateCounter<long>("payment.created", description: "Payments recorded");

    public static readonly Counter<long> PaymentAllocationCreated =
        Meter.CreateCounter<long>("payment.allocation.created", description: "Payment allocations created");

    public static readonly Counter<long> PaymentAllocationFailed =
        Meter.CreateCounter<long>("payment.allocation.failed", description: "Payment allocation validation failures");

    public static readonly Counter<long> PaymentReversal =
        Meter.CreateCounter<long>("payment.reversal", description: "Payment reversals");

    public static readonly Counter<long> PaymentAllocationReversal =
        Meter.CreateCounter<long>("payment.allocation.reversal", description: "Payment allocation reversals");
}
