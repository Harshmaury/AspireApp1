extern alias ExaminationAPI;
using AppHost.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Examination.Infrastructure.Persistence;
using Xunit;

namespace AppHost.IntegrationTests.Examination;

[Collection("ExaminationSuite")]
public class ExaminationIntegrationTests : IClassFixture<ServiceFixture<ExaminationAPI::Program, ExaminationDbContext>>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId  = TestTenant.Id;
    private readonly Guid _courseId  = Guid.NewGuid();
    private readonly Guid _enteredBy = Guid.NewGuid();

    public ExaminationIntegrationTests(ServiceFixture<ExaminationAPI::Program, ExaminationDbContext> fixture)
    {
        _client = fixture.Client;
    }

    private async Task<Guid> CreateScheduleAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/exam-schedules/", new
        {
            tenantId = _tenantId, courseId = _courseId, academicYear = "2025-26",
            semester = 1, examType = "EndSem", examDate = DateTime.UtcNow.AddDays(7),
            duration = 180, venue = "Hall A", maxMarks = 100, passingMarks = 40
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> EnterMarksAsync(Guid scheduleId, decimal marks = 65)
    {
        var resp = await _client.PostAsJsonAsync("/api/marks/", new
        {
            tenantId = _tenantId, studentId = Guid.NewGuid(),
            examScheduleId = scheduleId, courseId = _courseId,
            marksObtained = marks, maxMarks = 100, isAbsent = false, enteredBy = _enteredBy
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task CreateExamSchedule_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/exam-schedules/", new
        {
            tenantId = _tenantId, courseId = Guid.NewGuid(), academicYear = "2025-26",
            semester = 2, examType = "MidSem", examDate = DateTime.UtcNow.AddDays(14),
            duration = 120, venue = "Hall B", maxMarks = 50, passingMarks = 20
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task EnterMarks_Valid_Returns201()
    {
        var scheduleId = await CreateScheduleAsync();
        var response   = await _client.PostAsJsonAsync("/api/marks/", new
        {
            tenantId = _tenantId, studentId = Guid.NewGuid(), examScheduleId = scheduleId,
            courseId = _courseId, marksObtained = 78, maxMarks = 100,
            isAbsent = false, enteredBy = _enteredBy
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SubmitMarks_Returns204()
    {
        var scheduleId = await CreateScheduleAsync();
        var entryId    = await EnterMarksAsync(scheduleId);
        var response   = await _client.PutAsync(
            $"/api/marks/{entryId}/submit?tenantId={_tenantId}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SubmitAndApproveMarks_Returns204()
    {
        var scheduleId = await CreateScheduleAsync();
        var entryId    = await EnterMarksAsync(scheduleId);
        var approvedBy = Guid.NewGuid();
        await _client.PutAsync($"/api/marks/{entryId}/submit?tenantId={_tenantId}", null);
        var response = await _client.PutAsync(
            $"/api/marks/{entryId}/approve?tenantId={_tenantId}&approvedBy={approvedBy}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task FullMarksFlow_SubmitApprovePublish_Returns204()
    {
        var scheduleId = await CreateScheduleAsync();
        var entryId    = await EnterMarksAsync(scheduleId, 82);
        var approvedBy = Guid.NewGuid();
        await _client.PutAsync($"/api/marks/{entryId}/submit?tenantId={_tenantId}", null);
        await _client.PutAsync($"/api/marks/{entryId}/approve?tenantId={_tenantId}&approvedBy={approvedBy}", null);
        var response = await _client.PutAsync(
            $"/api/marks/{entryId}/publish?tenantId={_tenantId}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
