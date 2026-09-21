using Azure.Monitor.OpenTelemetry.AspNetCore;
using CareHome.Api.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace CareHome.Api.Telemetry;

public static class CareHomeTelemetryExtensions
{
    public static IServiceCollection AddCareHomeTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var serviceName = configuration["Telemetry:ServiceName"] ?? "CareHome.Api";
        var serviceVersion = typeof(CareHomeTelemetryExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";

        var appInsightsConnectionString =
            configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? configuration["ApplicationInsights:ConnectionString"];

        var telemetry = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = false;
                    })
                    .AddSource(CareHomeTelemetry.ActivitySourceName);

                if (string.IsNullOrWhiteSpace(appInsightsConnectionString)
                    && (environment.IsDevelopment()
                        || configuration.GetValue("Telemetry:EnableConsoleExporter", false)))
                {
                    tracing.AddConsoleExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddMeter(CareHomeTelemetry.MeterName);

                if (string.IsNullOrWhiteSpace(appInsightsConnectionString)
                    && (environment.IsDevelopment()
                        || configuration.GetValue("Telemetry:EnableConsoleExporter", false)))
                {
                    metrics.AddConsoleExporter();
                }
            });

        if (!string.IsNullOrWhiteSpace(appInsightsConnectionString))
        {
            telemetry.UseAzureMonitor();
        }

        return services;
    }
}
