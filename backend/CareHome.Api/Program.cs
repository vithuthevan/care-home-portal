using System.Text;
using System.Threading.RateLimiting;
using CareHome.Api.Audit;
using CareHome.Api.Billing.DependencyInjection;
using CareHome.Api.Common;
using CareHome.Api.Features;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Email;
using CareHome.Api.Export;
using CareHome.Api.Funding.DependencyInjection;
using CareHome.Api.Payments.DependencyInjection;
using CareHome.Api.Reconciliation.DependencyInjection;
using CareHome.Api.Remittance.DependencyInjection;
using CareHome.Api.RevenueAssurance.DependencyInjection;
using CareHome.Api.Receivables.DependencyInjection;
using CareHome.Api.Security;
using CareHome.Api.Security.Authorization;
using CareHome.Api.Services;
using CareHome.Api.Middleware;
using CareHome.Api.Telemetry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var applyMigrationsOnly = args.Contains("--apply-migrations", StringComparer.OrdinalIgnoreCase);
if (applyMigrationsOnly)
{
    var migrateConnectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' was not found.");

    ProductionStartupValidator.ValidateConnectionString(migrateConnectionString);

    builder.Services.AddDbContext<CareHomeDbContext>(options =>
        options.UseSqlServer(migrateConnectionString));

    var migrateApp = builder.Build();
    using var migrateScope = migrateApp.Services.CreateScope();
    var migrateDb = migrateScope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
    var migrateLogger = migrateScope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");
    await migrateDb.Database.MigrateAsync();
    await DatabaseMigrationStartupLogger.LogPendingMigrationsAsync(migrateDb, migrateLogger);
    migrateLogger.LogInformation("Database migrations applied (--apply-migrations).");
    return;
}

QuestPdfLicenseConfigurator.Configure(builder.Configuration);

builder.Services.Configure<CommercialRevenueFeature>(
    builder.Configuration.GetSection(CommercialRevenueFeature.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddCareHomeTelemetry(builder.Configuration, builder.Environment);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2 * 1024 * 1024;
    options.ValueLengthLimit = 1024 * 1024;
});

builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
    options.Filters.Add<ReadOnlyGuardFilter>();
});

var corsOrigins = ProductionStartupValidator.ResolveOrigins(builder.Configuration);
if (corsOrigins.Length == 0 && builder.Environment.IsDevelopment())
{
    corsOrigins = ["http://localhost:4200", "http://127.0.0.1:4200"];
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        if (corsOrigins.Length == 0)
        {
            policy.SetIsOriginAllowed(_ => false)
                .AllowAnyHeader()
                .AllowAnyMethod();
            return;
        }

        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found."
    );

builder.Services.AddDbContext<CareHomeDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<CareHomeDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = JwtSigningKey.Resolve(
    builder.Configuration["Jwt:Key"],
    builder.Environment.IsDevelopment());

var clockSkewMinutes = 2;
if (int.TryParse(builder.Configuration["Jwt:ClockSkewMinutes"], out var configuredSkew)
    && configuredSkew is >= 0 and <= 5)
{
    clockSkewMinutes = configuredSkew;
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.FromMinutes(clockSkewMinutes),
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CareHomeApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CareHomeWeb",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = JwtSecurityStamp.OnTokenValidated
    };
});

builder.Services.AddCareHomeAuthorizationPolicies();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<SqlReadyHealthCheck>("database", tags: ["ready"]);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (builder.Environment.IsDevelopment())
    {
        // Local reverse proxies / ngrok without static known IPs.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
    else
    {
        // Keep default loopback trust; only add explicitly configured proxies.
        var proxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>()
            ?? [];
        foreach (var proxy in proxies)
        {
            if (System.Net.IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }
    }
});

builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<IAuditWriter>(sp => sp.GetRequiredService<AuditService>());
builder.Services.AddScoped<UserAccessService>();
builder.Services.AddScoped<ICareHomeAccessScope>(sp => sp.GetRequiredService<UserAccessService>());
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddScoped<DocumentSequenceService>();
builder.Services.AddScoped<IDocumentSequence>(sp => sp.GetRequiredService<DocumentSequenceService>());
builder.Services.AddScoped<ClientIdentifierService>();
builder.Services.AddCareHomeFunding();
builder.Services.AddCareHomeBilling();
builder.Services.AddCareHomeReceivables();
builder.Services.AddCareHomePayments();
builder.Services.AddCareHomeReconciliation();
builder.Services.AddCareHomeRemittance();
builder.Services.AddCareHomeRevenueAssurance();
builder.Services.AddScoped<DisputeWorkflowService>();
builder.Services.AddScoped<ContractRenewalWorkflowService>();
builder.Services.AddScoped<CollectionsWorkflowService>();
builder.Services.AddScoped<FinanceAttentionService>();
builder.Services.AddScoped<InvoiceReceivableReadModel>();
builder.Services.AddScoped<InvoicePdfService>();
builder.Services.AddScoped<IDocumentStore, LocalDocumentStore>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddScoped<IEmailSender, ConfigurableEmailSender>();
builder.Services.AddScoped<Sage50ColumnMap>();
builder.Services.AddScoped<SageExportService>();
builder.Services.AddScoped<MasterDataUsageService>();
builder.Services.AddScoped<MiscChargeImportService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<DevelopmentMasterDataSeeder>();
builder.Services.AddScoped<EmptyTenantMasterDataSeeder>();
builder.Services.AddSingleton<LoginPasswordCipher>();

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
ProductionStartupValidator.Validate(app.Configuration, app.Environment, startupLogger);

app.UseExceptionHandler();
app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment()
    && app.Configuration.GetValue("Https:Redirect", true))
{
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseCors("AllowAngularApp");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CommercialRevenueApiGateMiddleware>();
app.UseMiddleware<RequestLoggingScopeMiddleware>();
app.UseMiddleware<InactiveTenantMiddleware>();
app.UseMiddleware<MustChangePasswordMiddleware>();

// Same-origin Angular SPA (wwwroot). API and health stay on dedicated routes.
var wwwRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(wwwRoot))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/_dev/throw", (HttpContext _) =>
        throw new InvalidOperationException("UAT forced server error for diagnostics."))
        .AllowAnonymous();
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = WriteHealthResponse
}).AllowAnonymous();

if (Directory.Exists(wwwRoot))
{
    var spaIndexPath = Path.Combine(wwwRoot, "index.html");
    app.MapFallback(async (HttpContext context) =>
    {
        if (SecurityHeadersMiddleware.MatchesApiOrHealthPath(context.Request.Path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = "Not found." });
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(spaIndexPath);
    }).AllowAnonymous();
}

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    var db = scope.ServiceProvider.GetRequiredService<CareHomeDbContext>();
    if (app.Configuration.GetValue("Database:ApplyMigrations", false))
    {
        await db.Database.MigrateAsync();
    }

    await DatabaseMigrationStartupLogger.LogPendingMigrationsAsync(db, logger);

    try
    {
        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();

        var dataSeeder = scope.ServiceProvider.GetRequiredService<DevelopmentMasterDataSeeder>();
        await dataSeeder.SeedAsync();

        var emptyTenantSeeder = scope.ServiceProvider.GetRequiredService<EmptyTenantMasterDataSeeder>();
        await emptyTenantSeeder.SeedAsync();
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("Development platform admin", StringComparison.Ordinal))
    {
        throw;
    }
    catch (Exception ex)
    {
        logger.LogWarning(
            ex,
            "Startup seed skipped. Apply database migrations before running the API.");
    }
}

app.Run();

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = new
    {
        status = report.Status.ToString(),
        correlationId = CorrelationIdMiddleware.Get(context)
    };
    return context.Response.WriteAsJsonAsync(payload);
}

public partial class Program;
