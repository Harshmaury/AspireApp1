// UMS.SharedKernel/Domain/OutboxMessage.cs
// Canonical outbox message shared across all 9 services.
// ProcessedAt uses 'set' (not 'init') — relay must assign it after publish.
using System;

namespace UMS.SharedKernel.Domain;

public sealed class OutboxMessage
{
    public Guid            Id          { get; init; } = Guid.NewGuid();
    public string          EventType   { get; init; } = string.Empty;
    public string          Payload     { get; init; } = string.Empty;
    public string?         TenantId    { get; init; }
    public DateTimeOffset  OccurredAt  { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set;  }   // set — relay writes this after publish
}
