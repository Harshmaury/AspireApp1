// ─────────────────────────────────────────────────────────────────────────────
// GlobalUsings.cs — Kafka.IntegrationTests
// ─────────────────────────────────────────────────────────────────────────────
// ARCHITECTURE: OutboxMessage is a SharedKernel cross-cutting concern.
// Domain Common namespaces (Fee.Domain.Common, etc.) do NOT redefine it.
// These global aliases resolve OutboxMessage to the canonical SharedKernel type
// so test files can use short-form 'OutboxMessage' without ambiguity.
//
// Reference: Microsoft Clean Architecture — SharedKernel owns cross-cutting types.
// Reference: Google Engineering Practices — single source of truth for shared types.
// ─────────────────────────────────────────────────────────────────────────────

global using OutboxMessage = UMS.SharedKernel.Domain.OutboxMessage;
