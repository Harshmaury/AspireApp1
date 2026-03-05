extern alias FacultyAPI;
using AppHost.IntegrationTests.Helpers;
using Faculty.Infrastructure.Persistence;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AppHost.IntegrationTests.Faculty;

// TST-8 fix: HTTP integration tests for Faculty service
[Collection("FacultySuite")]
public class FacultyIntegrationTests : IClassFixture<ServiceFixture<FacultyAPI::Program, FacultyDbContext>>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = TestTenant.Id;

    public FacultyIntegrationTests(ServiceFixture<FacultyAPI::Program, FacultyDbContext> fixture)
    {
        _client = fixture.Client;
    }

    private async Task<Guid> CreateFacultyAsync(string? email = null)
    {
        var resp = await _client.PostAsJsonAsync("/api/faculty", new
        {
            tenantId              = _tenantId,
            userId                = Guid.NewGuid(),
            departmentId          = Guid.NewGuid(),
            employeeId            = $"EMP{Guid.NewGuid():N}"[..10],
            firstName             = "Test",
            lastName              = "Faculty",
            email                 = email ?? $"faculty-{Guid.NewGuid()}@uni.edu",
            designation           = "Assistant Professor",
            specialization        = "Computer Science",
            highestQualification  = "PhD",
            experienceYears       = 5,
            isPhD                 = true,
            joiningDate           = "2020-01-01"
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task CreateFaculty_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/faculty", new
        {
            tenantId              = _tenantId,
            userId                = Guid.NewGuid(),
            departmentId          = Guid.NewGuid(),
            employeeId            = $"EMP{Guid.NewGuid():N}"[..10],
            firstName             = "Anil",
            lastName              = "Kumar",
            email                 = $"anil-{Guid.NewGuid()}@uni.edu",
            designation           = "Professor",
            specialization        = "Mathematics",
            highestQualification  = "PhD",
            experienceYears       = 10,
            isPhD                 = true,
            joiningDate           = "2015-07-01"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFaculty_Existing_Returns200()
    {
        var id = await CreateFacultyAsync();
        var response = await _client.GetAsync($"/api/faculty/{id}?tenantId={_tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetFaculty_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/faculty/{Guid.NewGuid()}?tenantId={_tenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateDesignation_Valid_Returns200()
    {
        var id = await CreateFacultyAsync();
        var response = await _client.PatchAsJsonAsync($"/api/faculty/{id}/designation", new
        {
            tenantId    = _tenantId,
            designation = "Associate Professor"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AssignCourse_Valid_Returns201()
    {
        var facultyId = await CreateFacultyAsync();
        var response = await _client.PostAsJsonAsync("/api/faculty/assignments", new
        {
            tenantId     = _tenantId,
            facultyId    = facultyId,
            courseId     = Guid.NewGuid(),
            academicYear = "2025-26",
            semester     = 1
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }
}
