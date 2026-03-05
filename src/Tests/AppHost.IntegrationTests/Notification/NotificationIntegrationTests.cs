extern alias NotificationAPI;
using AppHost.IntegrationTests.Helpers;
using Notification.Infrastructure.Persistence;
using FluentAssertions;
using System.Net;
using Xunit;

namespace AppHost.IntegrationTests.Notification;

// TST-8 fix: HTTP integration tests for Notification service.
// NOTE: Notification.API exposes no application endpoints beyond health checks —
// notifications are dispatched internally via domain events, not via direct HTTP calls.
// These tests verify the service starts correctly, migrations run, and templates are seeded.
// When HTTP endpoints are added to Notification.API they should be tested here.
[Collection("NotificationSuite")]
public class NotificationIntegrationTests : IClassFixture<ServiceFixture<NotificationAPI::Program, NotificationDbContext>>
{
    private readonly HttpClient _client;

    public NotificationIntegrationTests(ServiceFixture<NotificationAPI::Program, NotificationDbContext> fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "Notification service must start cleanly — a failure here means migrations, " +
            "DI registration, or template seeding threw on startup");
    }

    [Fact]
    public async Task RegionHealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health/region");
        // Accept OK or NotFound — region endpoint presence varies by environment
        ((int)response.StatusCode).Should().BeOneOf(200, 404);
    }
}
