extern alias ExaminationAPI;
using AppHost.IntegrationTests.Helpers;
using Examination.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AppHost.IntegrationTests.Examination;

[Collection("ExaminationSuite")]
public class ExaminationIntegrationTests : IClassFixture<ServiceFixture<ExaminationAPI::Program, ExaminationDbContext>>
{
    private readonly HttpClient _client;
    private readonly ServiceFixture<ExaminationAPI::Program, ExaminationDbContext> _fixture;
    private readonly Guid _tenantId  = TestTenant.Id;
    private readonly Guid _courseId  = Guid.NewGuid();
    private readonly Guid _enteredBy = Guid.NewGuid();

    public ExaminationIntegrationTests(ServiceFixture<ExaminationAPI::Program, ExaminationDbContext> fixture)
    {
        _fixture = fixture;
        _client  = fixture.Client;
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
        var courseId = Guid.NewGuid();
        var response = await _client.PostAsJsonAsync("/api/exam-schedules/", new
        {
            tenantId = _tenantId, courseId = courseId, academicYear = "2025-26",
            semester = 2, examType = "MidSem", examDate = DateTime.UtcNow.AddDays(14),
            duration = 120, venue = "Hall B", maxMarks = 50, passingMarks = 20
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        id.Should().NotBeEmpty();

        // TST-1 fix: verify the entity actually survived a round-trip to the DB
        var getResponse = await _client.GetAsync($"/api/exam-schedules/{id}?tenantId={_tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "exam schedule must be readable from DB after creation — a 201 with no persisted row is a silent bug");
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        fetched.GetProperty("id").GetGuid().Should().Be(id);
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
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();

        // TST-1 fix: verify marks entry persisted
        var getResponse = await _client.GetAsync($"/api/marks/{id}?tenantId={_tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK,
            "marks entry must be readable from DB after creation");
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

        // TST-1 fix: verify final published state is actually in the DB
        // This catches EXM-3 — state transitions were silent no-ops without SaveChangesAsync
        var getResponse = await _client.GetAsync($"/api/marks/{entryId}?tenantId={_tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        fetched.GetProperty("status").GetString().Should().Be("Published",
            "marks must be in Published state in DB after full Submit→Approve→Publish flow");
    }
}
