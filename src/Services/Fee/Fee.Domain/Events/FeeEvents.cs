using MediatR;
using UMS.SharedKernel.Kafka;

namespace Fee.Domain.Events;

public sealed record FeePaymentReceivedEvent(Guid PaymentId, Guid StudentId, Guid TenantId, decimal AmountPaid) : INotification, ITenantedEvent;
public sealed record FeeDefaulterMarkedEvent(Guid StudentId, Guid TenantId, string AcademicYear) : INotification, ITenantedEvent;
public sealed record ScholarshipGrantedEvent(Guid ScholarshipId, Guid StudentId, Guid TenantId, decimal Amount) : INotification, ITenantedEvent;