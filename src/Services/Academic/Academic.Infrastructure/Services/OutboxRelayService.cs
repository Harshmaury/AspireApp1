// Academic.Infrastructure/Services/OutboxRelayService.cs
// Inherits all relay logic from UMS.SharedKernel.Infrastructure.OutboxRelayServiceBase<T>.
using Confluent.Kafka;
using Academic.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Infrastructure;
using UMS.SharedKernel.Kafka;

namespace Academic.Infrastructure.Services;

public sealed class OutboxRelayService : OutboxRelayServiceBase<AcademicDbContext>
{
    public OutboxRelayService(
        IServiceScopeFactory        scopeFactory,
        IProducer<Null, string>     producer,
        ILogger<OutboxRelayService> logger)
        : base(scopeFactory, producer, logger) { }

    protected override string TopicName => KafkaTopics.AcademicEvents;
}

