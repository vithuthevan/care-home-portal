namespace CareHome.Api.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Mode { get; set; } = "Development";

    /// <summary>
    /// When true, non-Smtp modes are permitted outside Development. Sends still fail visibly
    /// (Success=false) so invoices are not marked Sent. Requires explicit business sign-off.
    /// </summary>
    public bool AllowSimulationInProduction { get; set; }

    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "Care Home Billing";

    public SmtpOptions Smtp { get; set; } = new();

    public bool IsSmtpMode =>
        string.Equals(Mode, "Smtp", StringComparison.OrdinalIgnoreCase);

    public bool IsDevelopmentMode =>
        string.Equals(Mode, "Development", StringComparison.OrdinalIgnoreCase);
}

public class SmtpOptions
{
    public string? Host { get; set; }

    public int Port { get; set; } = 587;

    public string? User { get; set; }

    public string? Password { get; set; }

    public bool EnableSsl { get; set; } = true;
}
