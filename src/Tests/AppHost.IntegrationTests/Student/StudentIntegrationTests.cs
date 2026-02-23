extern alias StudentAPI;
using AppHost.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Student.Infrastructure.Persistence;
using Xunit;

namespace AppHost.IntegrationTests.Student;

[Collection("StudentSuite")]
public class StudentIntegrationTests : IClassFixture<ServiceFixture<StudentAPI::Program, StudentDbContext>>
{
    private readonly HttpClient _client;

    public StudentIntegrationTests(ServiceFixture<StudentAPI::Program, StudentDbContext> fixture)
    {
        _client = fixture.Client;
    }

    private async Task<(Guid Id, string StudentNumber)> CreateStudentAsync(string email, Guid? userId = null)
    {
        var resp = await _client.PostAsJsonAsync("/api/students", new
        {
            tenantId  = TestTenant.Id,
            userId    = userId ?? Guid.NewGuid(),
            firstName = "Test",
            lastName  = "User",
            email
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("studentId").GetGuid(),
                body.GetProperty("studentNumber").GetString()!);
    }

    [Fact]
    public async Task CreateStudent_ValidRequest_Returns201WithStudentNumber()
    {
        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            tenantId  = TestTenant.Id,
            userId    = Guid.NewGuid(),
            firstName = "Riya",
            lastName  = "Sharma",
            email     = $"riya-{Guid.NewGuid()}@university.edu"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("studentNumber").GetString().Should().StartWith("STU-");
    }

    [Fact]
    public async Task CreateStudent_DuplicateUserId_Returns500()
    {
        var userId = Guid.NewGuid();
        await CreateStudentAsync($"first-{Guid.NewGuid()}@university.edu", userId);
        var response = await _client.PostAsJsonAsync("/api/students", new
        {
            tenantId  = TestTenant.Id,
            userId,
            firstName = "Dup",
            lastName  = "User",
            email     = $"second-{Guid.NewGuid()}@university.edu"
        });
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetStudent_Existing_Returns200WithData()
    {
        var (id, _) = await CreateStudentAsync($"arjun-{Guid.NewGuid()}@university.edu");
        var response = await _client.GetAsync($"/api/students/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("firstName").GetString().Should().Be("Test");
    }

    [Fact]
    public async Task GetStudent_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/students/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateStudent_ThenCheckStudentNumber_StartsWithSTU()
    {
        var (_, studentNumber) = await CreateStudentAsync($"check-{Guid.NewGuid()}@university.edu");
        studentNumber.Should().StartWith("STU-");
    }

    [Fact]
    public async Task AdmitStudent_ValidStudent_Returns204()
    {
        var (id, _) = await CreateStudentAsync($"admit-{Guid.NewGuid()}@university.edu");
        var response = await _client.PutAsync($"/api/students/{id}/admit", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AdmitStudent_NotFound_Returns500()
    {
        var response = await _client.PutAsync($"/api/students/{Guid.NewGuid()}/admit", null);
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }
}

