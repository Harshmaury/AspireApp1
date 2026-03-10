// Examination.Infrastructure/Persistence/DomainEventDispatcherInterceptor.cs
// Inherits all logic from DomainEventDispatcherInterceptorBase.
// IMediator implements IPublisher — pass directly to base.
using MediatR;
using UMS.SharedKernel.Infrastructure;

namespace Examination.Infrastructure.Persistence;

public sealed class DomainEventDispatcherInterceptor : DomainEventDispatcherInterceptorBase
{
    public DomainEventDispatcherInterceptor(IPublisher publisher)
        : base(publisher) { }
}
