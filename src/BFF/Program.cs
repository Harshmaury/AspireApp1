using UMS.SharedKernel.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using BFF.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();

// Resilience pipeline — applied to all named clients
static void AddUmsResilience(ResiliencePipelineBuilder<HttpResponseMessage> b)
{
    b.AddTimeout(TimeSpan.FromSeconds(10));
    b.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType      = DelayBackoffType.Exponential,
        Delay            = TimeSpan.FromSeconds(2),
        UseJitter        = true
    });
    b.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio      = 0.5,
        MinimumThroughput = 5,
        SamplingDuration  = TimeSpan.FromSeconds(30),
        BreakDuration     = TimeSpan.FromSeconds(30)
    });
}

builder.Services.AddHttpClient("student",      c => c.BaseAddress = new Uri("http://student-api"))
    .AddResilienceHandler("ums", AddUmsResilience);
builder.Services.AddHttpClient("fee",          c => c.BaseAddress = new Uri("http://fee-api"))
    .AddResilienceHandler("ums", AddUmsResilience);
builder.Services.AddHttpClient("attendance",   c => c.BaseAddress = new Uri("http://attendance-api"))
    .AddResilienceHandler("ums", AddUmsResilience);
builder.Services.AddHttpClient("academic",     c => c.BaseAddress = new Uri("http://academic-api"))
    .AddResilienceHandler("ums", AddUmsResilience);
builder.Services.AddHttpClient("notification", c => c.BaseAddress = new Uri("http://notification-api"))
    .AddResilienceHandler("ums", AddUmsResilience);

var app = builder.Build();

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();

app.MapDashboardEndpoints();
app.MapProfileEndpoints();
app.MapDefaultEndpoints();

app.Run();
