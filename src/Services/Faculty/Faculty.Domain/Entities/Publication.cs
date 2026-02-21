using Faculty.Domain.Common;
using Faculty.Domain.Enums;
using Faculty.Domain.Events;
using Faculty.Domain.Exceptions;
namespace Faculty.Domain.Entities;
public sealed class Publication : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FacultyId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Journal { get; private set; } = default!;
    public int PublishedYear { get; private set; }
    public string? DOI { get; private set; }
    public PublicationType Type { get; private set; }
    public int CitationCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    private Publication() { }
    public static Publication Create(Guid tenantId, Guid facultyId, string title, string journal, int publishedYear, PublicationType type, string? doi = null)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new FacultyDomainException("INVALID_TITLE", "Publication title is required.");
        if (string.IsNullOrWhiteSpace(journal)) throw new FacultyDomainException("INVALID_JOURNAL", "Journal name is required.");
        if (publishedYear < 1900 || publishedYear > DateTime.UtcNow.Year) throw new FacultyDomainException("INVALID_YEAR", "Invalid publication year.");
        return new Publication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacultyId = facultyId,
            Title = title.Trim(),
            Journal = journal.Trim(),
            PublishedYear = publishedYear,
            Type = type,
            DOI = doi?.Trim(),
            CitationCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }
    public void UpdateCitations(int count)
    {
        if (count < 0) throw new FacultyDomainException("INVALID_CITATIONS", "Citation count cannot be negative.");
        CitationCount = count;
    }
}
