using UMS.SharedKernel.Domain;
using UMS.SharedKernel.Kafka;

namespace Fee.Domain.Events;

public sealed record FeePaymentReceivedEvent(Guid PaymentId, Guid StudentId, Guid TenantId, decimal AmountPaid) : IDomainEvent, ITenantedEvent;
public sealed record FeeDefaulterMarkedEvent(Guid StudentId, Guid TenantId, string AcademicYear) : IDomainEvent, ITenantedEvent;
public sealed record ScholarshipGrantedEvent(Guid ScholarshipId, Guid StudentId, Guid TenantId, decimal Amount) : IDomainEvent, ITenantedEvent;
