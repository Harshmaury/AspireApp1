// Identity.API/Services/OutboxRelayService.cs
// Inherits all relay logic from UMS.SharedKernel.Infrastructure.OutboxRelayServiceBase<T>.
using Confluent.Kafka;
using Identity.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Infrastructure;
using UMS.SharedKernel.Kafka;

namespace Identity.API.Services;

public sealed class OutboxRelayService : OutboxRelayServiceBase<ApplicationDbContext>
{
    public OutboxRelayService(
        IServiceScopeFactory        scopeFactory,
        IProducer<Null, string>     producer,
        ILogger<OutboxRelayService> logger)
        : base(scopeFactory, producer, logger) { }

    protected override string TopicName => KafkaTopics.IdentityEvents;
}
