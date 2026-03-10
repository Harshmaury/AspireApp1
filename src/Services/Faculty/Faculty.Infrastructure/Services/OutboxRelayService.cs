// Faculty.Infrastructure/Services/OutboxRelayService.cs
// Inherits all relay logic from UMS.SharedKernel.Infrastructure.OutboxRelayServiceBase<T>.
using Confluent.Kafka;
using Faculty.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Infrastructure;
using UMS.SharedKernel.Kafka;

namespace Faculty.Infrastructure.Services;

public sealed class OutboxRelayService : OutboxRelayServiceBase<FacultyDbContext>
{
    public OutboxRelayService(
        IServiceScopeFactory        scopeFactory,
        IProducer<Null, string>     producer,
        ILogger<OutboxRelayService> logger)
        : base(scopeFactory, producer, logger) { }

    protected override string TopicName => KafkaTopics.FacultyEvents;
}

