# ============================================================
# Phase 5 — ServiceDefaults: GlobalExceptionMiddleware
#            + MigrationHostedService<TContext>
# ============================================================

$ErrorActionPreference = "Stop"
$root   = "C:\Users\harsh\source\repos\AspireApp1"
$backup = "$root\_backups\phase5-servicedefaults-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backup -Force | Out-Null

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 5 — ServiceDefaults: Middleware + MigrationHostedService" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# ── Locate ServiceDefaults project ───────────────────────────
$sdProj = Get-ChildItem "$root\src" -Recurse -Filter "*.ServiceDefaults.csproj" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1

if (-not $sdProj) {
    # Try Aspire naming convention
    $sdProj = Get-ChildItem "$root" -Recurse -Filter "*ServiceDefaults*.csproj" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1
}

if (-not $sdProj) {
    Write-Host "  ERROR: ServiceDefaults project not found." -ForegroundColor Red
    Write-Host "  Searched for *.ServiceDefaults.csproj under $root" -ForegroundColor Yellow
    exit 1
}

$sdDir = $sdProj.DirectoryName
Write-Host "  ServiceDefaults: $sdDir" -ForegroundColor DarkGray

# ── [1] GlobalExceptionMiddleware ────────────────────────────
$gemPath = "$sdDir\GlobalExceptionMiddleware.cs"
if (-not (Test-Path $gemPath)) {
    Set-Content -Path $gemPath -Encoding UTF8 -Value @"
// ServiceDefaults/GlobalExceptionMiddleware.cs
//
// Shared exception middleware registered via AddServiceDefaults().
// Replaces 9 per-service copies with one canonical implementation.

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspireApp1.ServiceDefaults;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning(vex, "Validation failure");
            ctx.Response.StatusCode  = (int)HttpStatusCode.BadRequest;
            ctx.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type   = "https://tools.ietf.org/html/rfc7807",
                title  = "Validation Error",
                status = 400,
                errors = vex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex) when (GetStatusCode(ex) is int code)
        {
            _logger.LogWarning(ex, "Domain/business error");
            ctx.Response.StatusCode  = code;
            ctx.Response.ContentType = "application/problem+json";

            var problem = new { type = "about:blank", title = ex.Message, status = code };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            ctx.Response.ContentType = "application/problem+json";

            var problem = new { type = "about:blank", title = "An unexpected error occurred.", status = 500 };
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }

    private static int? GetStatusCode(Exception ex) => ex switch
    {
        // Extend with domain exceptions as needed.
        // e.g. NotFoundException => 404, ConflictException => 409
        ArgumentException    => 400,
        UnauthorizedAccessException => 401,
        InvalidOperationException   => 409,
        _ => null
    };
}
"@
    Write-Host "  [1/3] Created: ServiceDefaults\GlobalExceptionMiddleware.cs" -ForegroundColor Green
} else {
    Write-Host "  [1/3] GlobalExceptionMiddleware.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── [2] MigrationHostedService<TContext> ─────────────────────
$migPath = "$sdDir\MigrationHostedService.cs"
if (-not (Test-Path $migPath)) {
    Set-Content -Path $migPath -Encoding UTF8 -Value @"
// ServiceDefaults/MigrationHostedService.cs
//
// Generic EF Core migration runner. Register once per service:
//   builder.Services.AddHostedService<MigrationHostedService<MyDbContext>>();
// Replaces 9 per-service copies.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspireApp1.ServiceDefaults;

public sealed class MigrationHostedService<TContext> : IHostedService
    where TContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MigrationHostedService<TContext>> _logger;

    public MigrationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MigrationHostedService<TContext>> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Migration:{Context}] Applying migrations...", typeof(TContext).Name);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            await db.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("[Migration:{Context}] Migrations applied.", typeof(TContext).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Migration:{Context}] Migration failed.", typeof(TContext).Name);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
"@
    Write-Host "  [2/3] Created: ServiceDefaults\MigrationHostedService.cs" -ForegroundColor Green
} else {
    Write-Host "  [2/3] MigrationHostedService.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── [3] Update Extensions.cs to register middleware ──────────
$extFile = "$sdDir\Extensions.cs"
if (Test-Path $extFile) {
    $content = Get-Content $extFile -Raw
    if ($content -notmatch "GlobalExceptionMiddleware") {
        Copy-Item $extFile "$backup\Extensions.cs"
        # Append a UseGlobalExceptionHandler extension note
        $appendix = @"

// ── Added by Phase 5 fix ──────────────────────────────────────
// Call app.UseGlobalExceptionHandler() after app.UseServiceDefaults()
// in every service Program.cs, replacing per-service middleware.
namespace AspireApp1.ServiceDefaults
{
    public static partial class ServiceDefaultsExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
"@
        Add-Content -Path $extFile -Value $appendix -Encoding UTF8
        Write-Host "  [3/3] Appended UseGlobalExceptionHandler() to Extensions.cs" -ForegroundColor Green
    } else {
        Write-Host "  [3/3] GlobalExceptionMiddleware already referenced in Extensions.cs — skipped" -ForegroundColor DarkGray
    }
} else {
    Write-Host "  [3/3] Extensions.cs not found — skipped (add UseGlobalExceptionHandler manually)" -ForegroundColor DarkYellow
}

# ── Report per-service files to clean up ─────────────────────
$services = @("Identity","Academic","Student","Attendance","Examination","Fee","Faculty","Hostel","Notification")
Write-Host ""
Write-Host "  Per-service files to remove after verification:" -ForegroundColor Yellow
foreach ($svc in $services) {
    $migs = Get-ChildItem "$root\src\Services\$svc" -Recurse -Include "*MigrationHostedService*.cs","*GlobalExceptionMiddleware*.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\obj\\" }
    foreach ($f in $migs) {
        Write-Host "    $($f.FullName)" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "  Phase 5 complete." -ForegroundColor Green
