// Attendance.Infrastructure/Services/OutboxRelayService.cs
// Inherits all relay logic from UMS.SharedKernel.Infrastructure.OutboxRelayServiceBase<T>.
using Confluent.Kafka;
using Attendance.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UMS.SharedKernel.Infrastructure;
using UMS.SharedKernel.Kafka;

namespace Attendance.Infrastructure.Services;

public sealed class OutboxRelayService : OutboxRelayServiceBase<AttendanceDbContext>
{
    public OutboxRelayService(
        IServiceScopeFactory        scopeFactory,
        IProducer<Null, string>     producer,
        ILogger<OutboxRelayService> logger)
        : base(scopeFactory, producer, logger) { }

    protected override string TopicName => KafkaTopics.AttendanceEvents;
}

