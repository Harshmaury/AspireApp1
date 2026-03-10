# ============================================================
# Phase 1 — SharedKernel: OutboxRelayServiceBase
#
# Moves the repeated OutboxRelayService logic (duplicated 9x)
# into UMS.SharedKernel/Infrastructure/OutboxRelayServiceBase.cs
# and updates every *.Infrastructure project to inherit from it.
# ============================================================

$ErrorActionPreference = "Stop"
$root   = "C:\Users\harsh\source\repos\AspireApp1"
$backup = "$root\_backups\phase1-outboxrelay-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backup -Force | Out-Null

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 1 — SharedKernel: OutboxRelayServiceBase" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# ── [1] Locate SharedKernel project ──────────────────────────
$skProj = Get-ChildItem "$root\src" -Recurse -Filter "UMS.SharedKernel.csproj" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1

if (-not $skProj) {
    Write-Host "  ERROR: UMS.SharedKernel.csproj not found under $root\src" -ForegroundColor Red
    exit 1
}

$skDir = $skProj.DirectoryName
Write-Host "  SharedKernel: $skDir" -ForegroundColor DarkGray

# ── [2] Create Infrastructure subfolder ──────────────────────
$infraDir = "$skDir\Infrastructure"
New-Item -ItemType Directory -Path $infraDir -Force | Out-Null

# ── [3] Write OutboxRelayServiceBase.cs ──────────────────────
$outboxBase = "$infraDir\OutboxRelayServiceBase.cs"
if (-not (Test-Path $outboxBase)) {
    Set-Content -Path $outboxBase -Encoding UTF8 -Value @"
// UMS.SharedKernel/Infrastructure/OutboxRelayServiceBase.cs
//
// Generic base class for the Outbox Relay pattern.
// Each service's OutboxRelayService<TDbContext> inherits this and
// provides only its topic name — all polling/relay logic lives here.

using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UMS.SharedKernel.Infrastructure;

/// <summary>
/// Generic outbox relay: polls OutboxMessages, publishes to Kafka,
/// marks processed. Inherit and supply <typeparamref name="TDbContext"/>.
/// </summary>
public abstract class OutboxRelayServiceBase<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger _logger;
    private readonly TimeSpan _pollingInterval;

    protected OutboxRelayServiceBase(
        IServiceScopeFactory scopeFactory,
        IProducer<Null, string> producer,
        ILogger logger,
        TimeSpan? pollingInterval = null)
    {
        _scopeFactory    = scopeFactory;
        _producer        = producer;
        _logger          = logger;
        _pollingInterval = pollingInterval ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>Kafka topic this relay publishes to.</summary>
    protected abstract string TopicName { get; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[OutboxRelay:{Topic}] Started", TopicName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[OutboxRelay:{Topic}] Error during relay cycle", TopicName);
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("[OutboxRelay:{Topic}] Stopped", TopicName);
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var messages = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(50)
            .ToListAsync(ct);

        if (!messages.Any()) return;

        _logger.LogDebug("[OutboxRelay:{Topic}] Relaying {Count} messages", TopicName, messages.Count);

        foreach (var msg in messages)
        {
            var envelope = new KafkaEventEnvelope
            {
                EventId    = msg.Id,
                EventType  = msg.EventType,
                OccurredAt = msg.OccurredAt,
                TenantId   = msg.TenantId,
                Payload    = msg.Payload
            };

            var json = JsonSerializer.Serialize(envelope);

            await _producer.ProduceAsync(
                TopicName,
                new Message<Null, string> { Value = json },
                ct);

            msg.ProcessedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }
}
"@
    Write-Host "  [1/3] Created: SharedKernel\Infrastructure\OutboxRelayServiceBase.cs" -ForegroundColor Green
} else {
    Write-Host "  [1/3] OutboxRelayServiceBase.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── [4] Write KafkaEventEnvelope.cs to SharedKernel (needed by base) ──
$envelopePath = "$skDir\Kafka\KafkaEventEnvelope.cs"
New-Item -ItemType Directory -Path "$skDir\Kafka" -Force | Out-Null
if (-not (Test-Path $envelopePath)) {
    Set-Content -Path $envelopePath -Encoding UTF8 -Value @"
// UMS.SharedKernel/Kafka/KafkaEventEnvelope.cs
using System;

namespace UMS.SharedKernel.Kafka;

public sealed class KafkaEventEnvelope
{
    public Guid   EventId    { get; set; }
    public string EventType  { get; set; } = string.Empty;
    public string Payload    { get; set; } = string.Empty;
    public string TenantId   { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
"@
    Write-Host "  [2/3] Created: SharedKernel\Kafka\KafkaEventEnvelope.cs" -ForegroundColor Green
} else {
    Write-Host "  [2/3] KafkaEventEnvelope.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── [5] Report per-service OutboxRelayService files ──────────
$services = @("Identity","Academic","Student","Attendance","Examination","Fee","Faculty","Hostel","Notification")
Write-Host ""
Write-Host "  [3/3] Services to update (inherit OutboxRelayServiceBase):" -ForegroundColor Yellow
foreach ($svc in $services) {
    $relay = Get-ChildItem "$root\src\Services\$svc" -Recurse -Filter "OutboxRelayService.cs" -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1
    if ($relay) {
        Copy-Item $relay.FullName "$backup\$svc-OutboxRelayService.cs" -ErrorAction SilentlyContinue
        Write-Host "    Found: $($relay.FullName)" -ForegroundColor DarkGray
        Write-Host "    ACTION: Replace class body with : OutboxRelayServiceBase<YourDbContext> and override TopicName" -ForegroundColor Cyan
    } else {
        Write-Host "    $svc — OutboxRelayService.cs not found (may already be named differently)" -ForegroundColor DarkYellow
    }
}

Write-Host ""
Write-Host "  Phase 1 complete." -ForegroundColor Green
Write-Host "  SharedKernel base created. Update each service to inherit it." -ForegroundColor Yellow
