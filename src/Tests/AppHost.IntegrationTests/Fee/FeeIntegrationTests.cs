extern alias FeeAPI;
using AppHost.IntegrationTests.Helpers;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Fee.Infrastructure.Persistence;
using Xunit;

namespace AppHost.IntegrationTests.Fee;

[Collection("FeeSuite")]
public class FeeIntegrationTests : IClassFixture<ServiceFixture<FeeAPI::Program, FeeDbContext>>
{
    private readonly HttpClient _client;
    private readonly Guid _tenantId = TestTenant.Id;

    public FeeIntegrationTests(ServiceFixture<FeeAPI::Program, FeeDbContext> fixture)
    {
        _client = fixture.Client;
    }

    private async Task<Guid> CreateFeeStructureAsync()
    {
        var resp = await _client.PostAsJsonAsync("/api/fee-structures/", new
        {
            tenantId       = _tenantId,
            programmeId    = Guid.NewGuid(),
            academicYear   = "2025-26",
            semester       = 1,
            tuitionFee     = 50000.00,
            examFee        = 5000.00,
            developmentFee = 3000.00,
            medicalFee     = 2000.00,
            dueDate        = DateTime.UtcNow.AddDays(30)
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    private async Task<Guid> CreateFeePaymentAsync(Guid structureId)
    {
        var resp = await _client.PostAsJsonAsync("/api/fee-payments/", new
        {
            tenantId       = _tenantId,
            studentId      = Guid.NewGuid(),
            feeStructureId = structureId,
            amountPaid     = 60000.00,
            paymentMode    = "Online"
        });
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task CreateFeeStructure_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/fee-structures/", new
        {
            tenantId = _tenantId, programmeId = Guid.NewGuid(), academicYear = "2025-26",
            semester = 2, tuitionFee = 50000.00, examFee = 5000.00,
            developmentFee = 3000.00, medicalFee = 2000.00, dueDate = DateTime.UtcNow.AddDays(30)
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateFeePayment_Valid_Returns201()
    {
        var structureId = await CreateFeeStructureAsync();
        var response = await _client.PostAsJsonAsync("/api/fee-payments/", new
        {
            tenantId = _tenantId, studentId = Guid.NewGuid(),
            feeStructureId = structureId, amountPaid = 60000.00, paymentMode = "Online"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task MarkPaymentSuccess_Returns204()
    {
        var structureId = await CreateFeeStructureAsync();
        var paymentId   = await CreateFeePaymentAsync(structureId);
        var response    = await _client.PutAsync(
            $"/api/fee-payments/{paymentId}/success?tenantId={_tenantId}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkPaymentFailed_Returns204()
    {
        var structureId = await CreateFeeStructureAsync();
        var paymentId   = await CreateFeePaymentAsync(structureId);
        var response    = await _client.PutAsync(
            $"/api/fee-payments/{paymentId}/failed?tenantId={_tenantId}", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateScholarship_Valid_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/scholarships/", new
        {
            tenantId = _tenantId, studentId = Guid.NewGuid(),
            name = "Merit Scholarship", amount = 10000.00, academicYear = "2025-26"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
