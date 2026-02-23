using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Microsoft.Extensions.Hosting;

public static class SerilogExtensions
{
    public static TBuilder AddSerilogDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                builder.Environment.IsDevelopment()
                    ? new Serilog.Formatting.Display.MessageTemplateTextFormatter(
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties}{NewLine}{Exception}", null)
                    : new CompactJsonFormatter())
            .CreateBootstrapLogger();

        builder.Services.AddSerilog((sp, lc) => lc
            .ReadFrom.Services(sp)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithCorrelationId()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .WriteTo.Console(
                builder.Environment.IsDevelopment()
                    ? new Serilog.Formatting.Display.MessageTemplateTextFormatter(
                        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties}{NewLine}{Exception}", null)
                    : new CompactJsonFormatter()));

        return builder;
    }

    public static WebApplication UseSerilogDefaults(this WebApplication app)
    {
        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
            opts.GetLevel = (ctx, elapsed, ex) =>
                ex != null || ctx.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : ctx.Response.StatusCode >= 400
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;
            opts.EnrichDiagnosticContext = (diag, ctx) =>
            {
                diag.Set("RequestHost", ctx.Request.Host.Value);
                diag.Set("RequestScheme", ctx.Request.Scheme);
                diag.Set("CorrelationId", ctx.TraceIdentifier);
            };
        });
        return app;
    }
}
