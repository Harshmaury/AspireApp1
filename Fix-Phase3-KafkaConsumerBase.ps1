# ============================================================
# Phase 3 — SharedKernel: KafkaConsumerBase + KafkaTopics
# ============================================================

$ErrorActionPreference = "Stop"
$root   = "C:\Users\harsh\source\repos\AspireApp1"
$backup = "$root\_backups\phase3-kafka-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backup -Force | Out-Null

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " PHASE 3 — SharedKernel: KafkaConsumerBase + Kafka types" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$skProj = Get-ChildItem "$root\src" -Recurse -Filter "UMS.SharedKernel.csproj" |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1
if (-not $skProj) { Write-Host "  ERROR: SharedKernel not found" -ForegroundColor Red; exit 1 }

$skDir    = $skProj.DirectoryName
$kafkaDir = "$skDir\Kafka"
New-Item -ItemType Directory -Path $kafkaDir -Force | Out-Null

# ── KafkaConsumerBase ────────────────────────────────────────
$consumerBase = "$kafkaDir\KafkaConsumerBase.cs"
if (-not (Test-Path $consumerBase)) {
    Set-Content -Path $consumerBase -Encoding UTF8 -Value @"
// UMS.SharedKernel/Kafka/KafkaConsumerBase.cs
//
// Generic Kafka consumer base. Each service consumer inherits this,
// supplies its topic + group-id, and implements HandleAsync.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UMS.SharedKernel.Kafka;

public abstract class KafkaConsumerBase<TEvent> : BackgroundService
    where TEvent : class
{
    private readonly ILogger _logger;
    private readonly string _bootstrapServers;

    protected KafkaConsumerBase(ILogger logger, string bootstrapServers)
    {
        _logger           = logger;
        _bootstrapServers = bootstrapServers;
    }

    protected abstract string TopicName   { get; }
    protected abstract string GroupId     { get; }

    protected abstract Task HandleAsync(KafkaEventEnvelope envelope, TEvent payload, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId          = GroupId,
            AutoOffsetReset  = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(TopicName);
        _logger.LogInformation("[KafkaConsumer:{Topic}] Subscribed (group={Group})", TopicName, GroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                if (result?.Message?.Value is null) continue;

                var envelope = JsonSerializer.Deserialize<KafkaEventEnvelope>(result.Message.Value);
                if (envelope is null) continue;

                var payload = JsonSerializer.Deserialize<TEvent>(envelope.Payload);
                if (payload is null) continue;

                await HandleAsync(envelope, payload, stoppingToken);
                consumer.Commit(result);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KafkaConsumer:{Topic}] Error processing message", TopicName);
            }
        }

        consumer.Close();
        _logger.LogInformation("[KafkaConsumer:{Topic}] Stopped", TopicName);
    }
}
"@
    Write-Host "  [1/2] Created: SharedKernel\Kafka\KafkaConsumerBase.cs" -ForegroundColor Green
} else {
    Write-Host "  [1/2] KafkaConsumerBase.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── KafkaTopics ──────────────────────────────────────────────
$topicsPath = "$kafkaDir\KafkaTopics.cs"
if (-not (Test-Path $topicsPath)) {
    Set-Content -Path $topicsPath -Encoding UTF8 -Value @"
// UMS.SharedKernel/Kafka/KafkaTopics.cs
// Single source of truth for all Kafka topic names.

namespace UMS.SharedKernel.Kafka;

public static class KafkaTopics
{
    public const string Identity     = "identity-events";
    public const string Student      = "student-events";
    public const string Academic     = "academic-events";
    public const string Attendance   = "attendance-events";
    public const string Examination  = "examination-events";
    public const string Fee          = "fee-events";
    public const string Faculty      = "faculty-events";
    public const string Hostel       = "hostel-events";
    public const string Notification = "notification-events";
}
"@
    Write-Host "  [2/2] Created: SharedKernel\Kafka\KafkaTopics.cs" -ForegroundColor Green
} else {
    Write-Host "  [2/2] KafkaTopics.cs already exists — skipped" -ForegroundColor DarkGray
}

# ── Locate existing KafkaConsumerBase in Notification ────────
$existing = Get-ChildItem "$root\src\Services\Notification" -Recurse -Filter "KafkaConsumerBase.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\obj\\" } | Select-Object -First 1
if ($existing) {
    Copy-Item $existing.FullName "$backup\Notification-KafkaConsumerBase.cs"
    Write-Host ""
    Write-Host "  Backed up original: $($existing.FullName)" -ForegroundColor DarkGray
    Write-Host "  ACTION: Delete the Notification-local copy and update its namespace reference." -ForegroundColor Cyan
}

Write-Host ""
Write-Host "  Phase 3 complete." -ForegroundColor Green
