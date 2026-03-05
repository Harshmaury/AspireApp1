extern alias AttendanceAPI;
using AppHost.IntegrationTests.Helpers;
using Attendance.Infrastructure.Persistence;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AppHost.IntegrationTests.Attendance;

// TST-8 fix: HTTP integration tests for Attendance service
[Collection("AttendanceSuite")]
public class AttendanceIntegrationTests : IClassFixture<ServiceFixture<AttendanceAPI::Program, AttendanceDbContext>>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = TestTenant.Id;

    public AttendanceIntegrationTests(ServiceFixture<AttendanceAPI::Program, AttendanceDbContext> fixture)
    {
        _client = fixture.Client;
    }

    private async Task<Guid> MarkAttendanceAsync(Guid? studentId = null, Guid? courseId = null)
    {
        var resp = await _client.PostAsJsonAsync("/api/attendance", new
        {
            tenantId     = _tenantId,
            studentId    = studentId ?? Guid.NewGuid(),
            courseId     = courseId  ?? Guid.NewGuid(),
            academicYear = "2025-26",
            semester     = 1,
            date         = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            classType    = "Lecture",
            isPresent    = true,
            markedBy     = Guid.NewGuid()
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task MarkAttendance_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/attendance", new
        {
            tenantId     = _tenantId,
            studentId    = Guid.NewGuid(),
            courseId     = Guid.NewGuid(),
            academicYear = "2025-26",
            semester     = 1,
            date         = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            classType    = "Lecture",
            isPresent    = true,
            markedBy     = Guid.NewGuid()
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStudentAttendance_Existing_Returns200()
    {
        var studentId = Guid.NewGuid();
        var courseId  = Guid.NewGuid();
        await MarkAttendanceAsync(studentId, courseId);

        var response = await _client.GetAsync(
            $"/api/attendance/student/{studentId}/course/{courseId}?tenantId={_tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetStudentAttendance_EmptyStudent_ReturnsEmptyData()
    {
        var response = await _client.GetAsync(
            $"/api/attendance/student/{Guid.NewGuid()}/course/{Guid.NewGuid()}?tenantId={_tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MarkCondonation_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/attendance/condonation", new
        {
            tenantId  = _tenantId,
            studentId = Guid.NewGuid(),
            courseId  = Guid.NewGuid(),
            reason    = "Medical emergency."
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPendingCondonations_Returns200()
    {
        var response = await _client.GetAsync($"/api/attendance/condonation/pending?tenantId={_tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
