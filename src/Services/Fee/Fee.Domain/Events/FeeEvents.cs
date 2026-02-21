using MediatR;
namespace Fee.Domain.Events;
public sealed record FeePaymentReceivedEvent(Guid PaymentId, Guid StudentId, Guid TenantId, decimal AmountPaid) : INotification;
public sealed record FeeDefaulterMarkedEvent(Guid StudentId, Guid TenantId, string AcademicYear) : INotification;
public sealed record ScholarshipGrantedEvent(Guid ScholarshipId, Guid StudentId, Guid TenantId, decimal Amount) : INotification;
