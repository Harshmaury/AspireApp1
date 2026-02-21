using MediatR;
using Attendance.Application.DTOs;
using Attendance.Application.Interfaces;
namespace Attendance.Application.AttendanceRecord.Queries;
public sealed class GetStudentAttendanceQueryHandler : IRequestHandler<GetStudentAttendanceQuery, List<AttendanceRecordDto>>
{
    private readonly IAttendanceRecordRepository _repository;
    public GetStudentAttendanceQueryHandler(IAttendanceRecordRepository repository) => _repository = repository;
    public async Task<List<AttendanceRecordDto>> Handle(GetStudentAttendanceQuery query, CancellationToken ct)
    {
        var records = await _repository.GetByStudentCourseAsync(query.StudentId, query.CourseId, query.TenantId, ct);
        return records.Select(r => new AttendanceRecordDto(r.Id, r.StudentId, r.CourseId, r.AcademicYear, r.Semester, r.Date, r.ClassType.ToString(), r.IsPresent, r.IsLocked)).ToList();
    }
}
public sealed class GetCourseAttendanceByDateQueryHandler : IRequestHandler<GetCourseAttendanceByDateQuery, List<AttendanceRecordDto>>
{
    private readonly IAttendanceRecordRepository _repository;
    public GetCourseAttendanceByDateQueryHandler(IAttendanceRecordRepository repository) => _repository = repository;
    public async Task<List<AttendanceRecordDto>> Handle(GetCourseAttendanceByDateQuery query, CancellationToken ct)
    {
        var records = await _repository.GetByCourseAndDateAsync(query.CourseId, query.Date, query.TenantId, ct);
        return records.Select(r => new AttendanceRecordDto(r.Id, r.StudentId, r.CourseId, r.AcademicYear, r.Semester, r.Date, r.ClassType.ToString(), r.IsPresent, r.IsLocked)).ToList();
    }
}
