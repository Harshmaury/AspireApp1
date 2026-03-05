using Microsoft.EntityFrameworkCore;
using Student.Application.Interfaces;
using Student.Domain.Entities;
using Student.Domain.Enums;
using Student.Infrastructure.Persistence;

namespace Student.Infrastructure.Persistence.Repositories;

internal sealed class StudentRepository : IStudentRepository
{
    private readonly StudentDbContext _db;
    public StudentRepository(StudentDbContext db) => _db = db;

    // WRITE PATH - tracked. Command handlers mutate this entity then call UpdateAsync.
    // No AsNoTracking: entity stays in ChangeTracker so the interceptor can
    // find DomainEvents via ChangeTracker.Entries<IAggregateRoot>() in SavingChangesAsync.
    public async Task<StudentAggregate?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    // WRITE PATH - tracked. Same reason as GetByIdAsync.
    public async Task<StudentAggregate?> GetByUserIdAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .FirstOrDefaultAsync(s => s.UserId == userId && s.TenantId == tenantId, ct);

    // READ-ONLY PATH - untracked. Query handlers read but never mutate.
    // AsNoTracking: faster, no identity map entry, no ChangeTracker pollution.
    public async Task<StudentAggregate?> GetByIdReadOnlyAsync(Guid id, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);

    // READ-ONLY - AnyAsync never materialises an entity, no tracking concern.
    public async Task<bool> ExistsAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _db.Students
            .AnyAsync(s => s.UserId == userId && s.TenantId == tenantId, ct);

    // READ-ONLY PATH - AsNoTracking correct here. Query handlers never mutate.
    public async Task<(List<StudentAggregate> Items, int TotalCount)> GetAllAsync(
        Guid tenantId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Students
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StudentStatus>(status, true, out var parsed))
            query = query.Where(s => s.Status == parsed);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    // ADD - EF tracks new entities automatically on AddAsync. Interceptor fires on SaveChanges.
    public async Task AddAsync(StudentAggregate student, CancellationToken ct = default)
    {
        await _db.Students.AddAsync(student, ct);
        await _db.SaveChangesAsync(ct);
    }

    // UPDATE - _db.Students.Update() removed.
    // Entity is already tracked from GetByIdAsync (no AsNoTracking).
    // EF change detection has already marked only mutated properties as Modified.
    // Calling Update() on a tracked entity marks ALL columns dirty - full overwrite -
    // which bypasses the xmin concurrency token and clobbers concurrent writes silently.
    public async Task UpdateAsync(StudentAggregate student, CancellationToken ct = default)
    {
        if (_db.Entry(student).State == EntityState.Detached)
            throw new InvalidOperationException(
                $"StudentAggregate '{student.Id}' is detached. " +
                "Command handlers must use GetByIdAsync (tracked). " +
                "Never pass an entity loaded via GetByIdReadOnlyAsync or GetAllAsync into UpdateAsync.");

        await _db.SaveChangesAsync(ct);
    }
}