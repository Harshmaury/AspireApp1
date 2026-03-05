extern alias HostelAPI;
using AppHost.IntegrationTests.Helpers;
using Hostel.Infrastructure.Persistence;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace AppHost.IntegrationTests.Hostel;

// TST-8 fix: HTTP integration tests for Hostel service
[Collection("HostelSuite")]
public class HostelIntegrationTests : IClassFixture<ServiceFixture<HostelAPI::Program, HostelDbContext>>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = TestTenant.Id;

    public HostelIntegrationTests(ServiceFixture<HostelAPI::Program, HostelDbContext> fixture)
    {
        _client = fixture.Client;
    }

    private async Task<Guid> CreateHostelAsync(string name = "Block A")
    {
        var resp = await _client.PostAsJsonAsync("/api/hostels/", new
        {
            name          = name,
            type          = 0,
            totalRooms    = 20,
            wardenName    = "Mr. Warden",
            wardenContact = "9999999999"
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("data").GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task CreateHostel_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/hostels/", new
        {
            name          = $"Block-{Guid.NewGuid():N}"[..10],
            type          = 0,
            totalRooms    = 30,
            wardenName    = "Mrs. Warden",
            wardenContact = "8888888888"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHostel_Existing_Returns200()
    {
        var id = await CreateHostelAsync($"Block-{Guid.NewGuid():N}"[..10]);
        var response = await _client.GetAsync($"/api/hostels/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetHostel_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/hostels/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateRoom_Valid_Returns201()
    {
        var hostelId = await CreateHostelAsync($"Block-{Guid.NewGuid():N}"[..10]);
        var response = await _client.PostAsJsonAsync("/api/rooms/", new
        {
            hostelId   = hostelId,
            roomNumber = "101",
            floor      = 1,
            type       = 0,
            capacity   = 2
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHostels_Returns200WithList()
    {
        await CreateHostelAsync($"Block-{Guid.NewGuid():N}"[..10]);
        var response = await _client.GetAsync("/api/hostels/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("success").GetBoolean().Should().BeTrue();
    }
}
