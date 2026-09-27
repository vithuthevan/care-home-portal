using CareHome.Api.Dtos.Collections;
using CareHome.Api.Services;
using Xunit;

namespace CareHome.Api.Tests;

public class CollectionPolicyValidatorTests
{
    [Fact]
    public void Accepts_default_threshold_ordering()
    {
        var request = new UpdateCollectionPolicyRequest
        {
            Overdue7Days = 7,
            Overdue14Days = 14,
            Overdue30Days = 30,
            EscalationDays = 60,
        };
        Assert.Null(CollectionPolicyValidator.Validate(request));
    }

    [Fact]
    public void Rejects_inverted_overdue_thresholds()
    {
        var request = new UpdateCollectionPolicyRequest
        {
            Overdue7Days = 14,
            Overdue14Days = 7,
            Overdue30Days = 30,
            EscalationDays = 60,
        };
        Assert.NotNull(CollectionPolicyValidator.Validate(request));
    }

    [Fact]
    public void Rejects_escalation_before_thirty_day_stage()
    {
        var request = new UpdateCollectionPolicyRequest
        {
            Overdue7Days = 7,
            Overdue14Days = 14,
            Overdue30Days = 30,
            EscalationDays = 20,
        };
        Assert.Contains("Escalation", CollectionPolicyValidator.Validate(request)!);
    }
}
