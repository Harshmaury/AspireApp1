namespace Identity.Application.DTOs;

public sealed record RegisterRequest(
    string TenantSlug,
    string Email,
    string Password,
    string FirstName,
    string LastName
);
