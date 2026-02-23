using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Identity.IntegrationTests.Fixtures;
using Xunit;

namespace Identity.IntegrationTests.Tests;

[Collection("IdentityIntegration")]
public sealed class OpenIddictTokenFlowTests
{
    private readonly HttpClient _client;

    private const string ValidUsername = "ums|superadmin@ums.com";
    private const string ValidPassword = "Admin@1234";
    private const string ClientId      = "api-gateway";
    private const string ClientSecret  = "api-gateway-secret";

    public OpenIddictTokenFlowTests(IdentityIntegrationFixture fixture)
    {
        _client = fixture.Client;
    }

    private static FormUrlEncodedContent PasswordGrantForm(string username, string password) =>
        new(new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["username"]      = username,
            ["password"]      = password,
            ["scope"]         = "openid offline_access"
        });

    private static FormUrlEncodedContent RefreshGrantForm(string refreshToken) =>
        new(new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["client_id"]     = ClientId,
            ["client_secret"] = ClientSecret,
            ["refresh_token"] = refreshToken
        });

    [Fact]
    public async Task PasswordGrant_ValidCredentials_Returns200WithAccessToken()
    {
        var response = await _client.PostAsync("/connect/token", PasswordGrantForm(ValidUsername, ValidPassword));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.TryGetProperty("access_token", out var token).Should().BeTrue();
        token.GetString().Should().NotBeNullOrWhiteSpace();
        json.TryGetProperty("token_type", out var type).Should().BeTrue();
        type.GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task PasswordGrant_InvalidPassword_Returns400WithInvalidGrant()
    {
        var response = await _client.PostAsync("/connect/token", PasswordGrantForm(ValidUsername, "WrongPassword!99"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetString().Should().Be("invalid_grant");
    }

    [Fact]
    public async Task PasswordGrant_UnknownUser_Returns400WithInvalidGrant()
    {
        var response = await _client.PostAsync("/connect/token", PasswordGrantForm("ghost@ums.local", "Any@123!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetString().Should().Be("invalid_grant");
    }

    [Fact]
    public async Task RefreshToken_ValidRefreshToken_ReturnsNewAccessToken()
    {
        var loginResponse = await _client.PostAsync("/connect/token", PasswordGrantForm(ValidUsername, ValidPassword));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync()).RootElement;
        loginJson.TryGetProperty("refresh_token", out var rt).Should().BeTrue("scope must include offline_access");
        var refreshToken = rt.GetString()!;

        var refreshResponse = await _client.PostAsync("/connect/token", RefreshGrantForm(refreshToken));

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshJson = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync()).RootElement;
        refreshJson.TryGetProperty("access_token", out var newToken).Should().BeTrue();
        newToken.GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RefreshToken_FakeToken_Returns400()
    {
        var response = await _client.PostAsync("/connect/token", RefreshGrantForm("this.is.a.fake.refresh.token"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        json.TryGetProperty("error", out var error).Should().BeTrue();
        error.GetString().Should().BeOneOf("invalid_grant", "invalid_token");
    }
}
