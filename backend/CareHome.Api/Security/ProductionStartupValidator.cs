using CareHome.Api.Email;

namespace CareHome.Api.Security;

public static class ProductionStartupValidator
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment, ILogger logger)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        ValidateConnectionString(configuration.GetConnectionString("DefaultConnection"));
        ValidateEmail(configuration, logger);
        ValidateCors(configuration, environment);
        ValidateProductionSeed(configuration);
    }

    public static void ValidateConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not configured. Set ConnectionStrings__DefaultConnection to a SQL Server connection string.");
        }

        if (connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("MSSQLLocalDB", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "LocalDB is not allowed outside Development. Set ConnectionStrings__DefaultConnection to a SQL Server instance.");
        }
    }

    public static void ValidateEmail(IConfiguration configuration, ILogger logger)
    {
        var options = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();

        if (options.IsSmtpMode)
        {
            if (string.IsNullOrWhiteSpace(options.Smtp.Host) || string.IsNullOrWhiteSpace(options.FromAddress))
            {
                throw new InvalidOperationException(
                    "Email:Mode is Smtp but Email:Smtp:Host or Email:FromAddress is missing. Set Email__Smtp__Host and Email__FromAddress.");
            }

            if (!string.IsNullOrWhiteSpace(options.Smtp.User) && string.IsNullOrWhiteSpace(options.Smtp.Password))
            {
                throw new InvalidOperationException(
                    "Email:Mode is Smtp and Email:Smtp:User is set but Email:Smtp:Password is missing. Store the password in Key Vault as Email-Smtp-Password and reference it from Email__Smtp__Password.");
            }

            logger.LogInformation(
                "Email mode is Smtp. Host={Host} Port={Port} From={From} Ssl={Ssl} Auth={Auth}",
                options.Smtp.Host,
                options.Smtp.Port > 0 ? options.Smtp.Port : 587,
                options.FromAddress,
                options.Smtp.EnableSsl,
                string.IsNullOrWhiteSpace(options.Smtp.User) ? "anonymous" : "username");
            return;
        }

        if (options.AllowSimulationInProduction)
        {
            logger.LogWarning(
                "PRODUCTION EMAIL SIMULATION EXPLICITLY ALLOWED (Email:AllowSimulationInProduction=true). Email:Mode={Mode}. Sends will fail visibly; use the manual-send workflow. Configure Email__Mode=Smtp for live delivery.",
                string.IsNullOrWhiteSpace(options.Mode) ? "(empty)" : options.Mode);
            return;
        }

        throw new InvalidOperationException(
            "Production requires Email:Mode=Smtp for live email delivery. Set Email__Mode=Smtp with Email__Smtp__Host, Email__FromAddress, and Email__Smtp__Password (Key Vault reference). " +
            "For an interim manual-send workflow only, set Email__AllowSimulationInProduction=true with business sign-off (see docs/operations/PRODUCTION_EMAIL_SETUP.md).");
    }

    public static void ValidateCors(IConfiguration configuration, IHostEnvironment environment)
    {
        var origins = ResolveOrigins(configuration);
        if (origins.Any(o => string.Equals(o, "*", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("CORS must not use AllowAnyOrigin / '*'.");
        }

        if (!environment.IsDevelopment()
            && origins.Any(o => o.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || o.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Production CORS must not include localhost. Set Cors__AllowedOrigins__0 to the HTTPS origin of the Angular host, or leave origins empty for same-origin hosting.");
        }
    }

    public static string[] ResolveOrigins(IConfiguration configuration)
    {
        var allowed = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (allowed.Length > 0)
        {
            return allowed.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
        }

        var legacy = configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
        return legacy.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();
    }

    public static void ValidateProductionSeed(IConfiguration configuration)
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (KnownDevelopmentCredentials.IsForbiddenProductionBootstrap(email, password))
        {
            throw new InvalidOperationException(
                "The Development platform admin credentials cannot be used outside Development. Set Seed__AdminEmail and Seed__AdminPassword to unique bootstrap values, or leave them empty and create the first PlatformAdmin manually.");
        }
    }
}
