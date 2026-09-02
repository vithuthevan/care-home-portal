using System.Text;
using System.Threading.RateLimiting;
using CareHome.Api.Audit;
using CareHome.Api.Billing;
using CareHome.Api.Common;
using CareHome.Api.Data;
using CareHome.Api.Documents;
using CareHome.Api.Email;
using CareHome.Api.Export;
using CareHome.Api.Security;
using CareHome.Api.Services;
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

builder.Services.AddHttpContextAccessor();
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
});

builder.Services.AddAuthorization();

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
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<UserAccessService>();
builder.Services.AddScoped<TenantProvisioningService>();
builder.Services.AddScoped<DocumentSequenceService>();
builder.Services.AddScoped<RateCalculator>();
builder.Services.AddScoped<InvoiceTemplateResolver>();
builder.Services.AddScoped<BillingService>();
builder.Services.AddScoped<CreditNoteService>();
builder.Services.AddScoped<InvoicePdfService>();
builder.Services.AddScoped<IDocumentStore, LocalDocumentStore>();
builder.Services.AddScoped<IEmailSender, ConfigurableEmailSender>();
builder.Services.AddScoped<Sage50ColumnMap>();
builder.Services.AddScoped<SageExportService>();
builder.Services.AddScoped<MiscChargeImportService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<IdentitySeeder>();
builder.Services.AddScoped<DevelopmentMasterDataSeeder>();
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
app.UseMiddleware<RequestLoggingScopeMiddleware>();
app.UseMiddleware<InactiveTenantMiddleware>();
app.UseMiddleware<MustChangePasswordMiddleware>();
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

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    try
    {
        var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await identitySeeder.SeedAsync();

        var dataSeeder = scope.ServiceProvider.GetRequiredService<DevelopmentMasterDataSeeder>();
        await dataSeeder.SeedAsync();
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
