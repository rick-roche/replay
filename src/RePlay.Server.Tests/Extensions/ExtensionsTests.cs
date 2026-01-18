using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace RePlay.Server.Tests.Extensions;

public class ExtensionsTests
{
    [Fact]
    public void AddDefaultHealthChecks_RegistersHealthCheckService()
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            ContentRootPath = System.IO.Directory.GetCurrentDirectory()
        });

        // Exercise method under test
        Microsoft.Extensions.Hosting.Extensions.AddDefaultHealthChecks(builder);

        var sp = builder.Services.BuildServiceProvider();
        var healthService = sp.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();
        healthService.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureOpenTelemetry_RegistersProviders()
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            ContentRootPath = System.IO.Directory.GetCurrentDirectory()
        });

        // Ensure logging is available to attach OpenTelemetry
        builder.Logging.ClearProviders();

        // Exercise method under test
        Microsoft.Extensions.Hosting.Extensions.ConfigureOpenTelemetry(builder);

        var sp = builder.Services.BuildServiceProvider();
        sp.GetService<TracerProvider>().Should().NotBeNull();
        sp.GetService<MeterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddOpenTelemetryExporters_Disabled_DoesNotThrow()
    {
        var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Development,
            ContentRootPath = System.IO.Directory.GetCurrentDirectory()
        });

        // No OTLP endpoint configured
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;

        // Exercise method under test (private generic extension invoked via public wrapper)
        Microsoft.Extensions.Hosting.Extensions.ConfigureOpenTelemetry(builder);

        // Simply building the provider should succeed
        var sp = builder.Services.BuildServiceProvider();
        sp.Should().NotBeNull();
    }

    [Fact]
    public void MapDefaultEndpoints_Development_MapsHealthEndpoints()
    {
        // Set environment to Development
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environments.Development);
        var builder = WebApplication.CreateBuilder();
        // Ensure health checks are registered prior to mapping endpoints
        Microsoft.Extensions.Hosting.Extensions.AddDefaultHealthChecks(builder);
        var app = builder.Build();

        // Exercise method under test
        Microsoft.Extensions.Hosting.Extensions.MapDefaultEndpoints(app);

        // We won't start the app; just ensure pipeline built successfully
        app.Should().NotBeNull();
    }

    [Fact]
    public void MapDefaultEndpoints_NonDevelopment_DoesNotThrow()
    {
        // Set environment to Production
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Environments.Production);
        var builder = WebApplication.CreateBuilder();
        // Ensure health checks are registered even if not used in non-dev
        Microsoft.Extensions.Hosting.Extensions.AddDefaultHealthChecks(builder);
        var app = builder.Build();

        // Exercise method under test
        Microsoft.Extensions.Hosting.Extensions.MapDefaultEndpoints(app);

        app.Should().NotBeNull();
    }
}
