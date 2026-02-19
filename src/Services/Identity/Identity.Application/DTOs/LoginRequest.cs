namespace Identity.Application.DTOs;

public sealed record LoginRequest(
    string TenantSlug,
    string Email,
    string Password
);
