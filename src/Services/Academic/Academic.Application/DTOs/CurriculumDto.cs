namespace Academic.Application.DTOs;
public sealed record CurriculumDto(Guid Id, Guid ProgrammeId, Guid CourseId, int Semester, bool IsElective, bool IsOptional, string Version);