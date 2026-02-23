namespace Notification.Domain.Exceptions;
public sealed class NotificationDomainException : Exception, UMS.SharedKernel.Exceptions.IDomainException
{
    public string Code { get; }
    public NotificationDomainException(string code, string message) : base(message) => Code = code;
}
