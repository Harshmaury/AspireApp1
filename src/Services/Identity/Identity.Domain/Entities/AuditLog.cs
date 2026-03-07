namespace Identity.Domain.Entities;

public sealed class AuditLog
{
    public Guid     Id         { get; init; } = Guid.NewGuid();
    public Guid     TenantId   { get; init; }
    public Guid?    UserId     { get; init; }
    public string   Action     { get; init; } = string.Empty;
    public string?  IpAddress  { get; init; }
    public string?  UserAgent  { get; init; }
    public string?  Details    { get; init; }
    public bool     Succeeded  { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    private AuditLog() { }

    public static AuditLog Create(
        string  action,
        Guid    tenantId,
        Guid?   userId    = null,
        bool    succeeded = true,
        string? ip        = null,
        string? ua        = null,
        string? details   = null) => new()
    {
        Action     = action,
        TenantId   = tenantId,
        UserId     = userId,
        Succeeded  = succeeded,
        IpAddress  = ip,
        UserAgent  = ua,
        Details    = details
    };
}

public static class AuditActions
{
    public const string Login           = "LOGIN";
    public const string LoginFailed     = "LOGIN_FAILED";
    public const string Lockout         = "LOCKOUT";
    public const string Register        = "REGISTER";
    public const string EmailVerified   = "EMAIL_VERIFIED";
    public const string PasswordReset   = "PASSWORD_RESET";
    public const string PasswordChanged = "PASSWORD_CHANGED";
    public const string Deactivated     = "USER_DEACTIVATED";
    public const string TenantCreated   = "TENANT_CREATED";
    public const string TenantSuspended = "TENANT_SUSPENDED";
    public const string TokenRevoked    = "TOKEN_REVOKED";
}
