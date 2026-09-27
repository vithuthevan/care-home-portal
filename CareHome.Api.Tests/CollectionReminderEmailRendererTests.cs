using CareHome.Api.Email;
using Xunit;

namespace CareHome.Api.Tests;

public class CollectionReminderEmailRendererTests
{
    [Fact]
    public void Outstanding_amount_placeholder_uses_passed_balance()
    {
        var (_, body, _) = CollectionReminderEmailRenderer.Render(
            "Invoice {{InvoiceNumber}}",
            "Balance {{OutstandingAmount}}",
            "INV-1",
            new DateOnly(2026, 3, 1),
            5,
            1_234.5m,
            "£");

        Assert.Contains("£1,234.50", body);
    }
}
