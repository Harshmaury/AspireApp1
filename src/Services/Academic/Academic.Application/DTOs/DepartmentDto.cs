namespace Academic.Application.DTOs;
public sealed record DepartmentDto(Guid Id, Guid TenantId, string Name, string Code, string? Description, int EstablishedYear, string Status, DateTime CreatedAt);