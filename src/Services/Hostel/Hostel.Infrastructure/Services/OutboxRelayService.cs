// Hostel.Infrastructure/Services/OutboxRelayService.cs
// Inherits all relay logic from UMS.SharedKernel.Infrastructure.OutboxRelayServiceBase<T>.
using Confluent.Kafka;
using Hostel.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Infrastructure;
using UMS.SharedKernel.Kafka;

namespace Hostel.Infrastructure.Services;

public sealed class OutboxRelayService : OutboxRelayServiceBase<HostelDbContext>
{
    public OutboxRelayService(
        IServiceScopeFactory        scopeFactory,
        IProducer<Null, string>     producer,
        ILogger<OutboxRelayService> logger)
        : base(scopeFactory, producer, logger) { }

    protected override string TopicName => KafkaTopics.HostelEvents;
}

