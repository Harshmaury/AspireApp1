using UMS.SharedKernel.Domain;
namespace Faculty.Application.Interfaces;
public interface IFacultyEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent;
}
