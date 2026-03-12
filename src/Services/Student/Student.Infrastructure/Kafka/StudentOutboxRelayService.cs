using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Student.Infrastructure.Persistence;
using UMS.SharedKernel.Infrastructure;
using UMS.SharedKernel.Kafka;

namespace Student.Infrastructure.Kafka;

public sealed class StudentOutboxRelayService : OutboxRelayServiceBase<StudentDbContext>
{
    public StudentOutboxRelayService(
        IServiceScopeFactory scopeFactory,
        IProducer<Null, string> producer,
        ILogger<StudentOutboxRelayService> logger)
        : base(scopeFactory, producer, logger) { }

    protected override string TopicName => KafkaTopics.StudentEvents;
}

