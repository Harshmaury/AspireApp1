namespace Identity.Application.DTOs;

public sealed record AuthResponse(
    Guid UserId,
    string Email,
    string FullName,
    string AccessToken,
    string TokenType,
    int ExpiresIn
);
