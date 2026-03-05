using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Auth:RequireHttpsMetadata", false);
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

// FIX G2: AllowAnyOrigin() was used in all environments — allows any browser origin
// including attacker-controlled sites to make credentialed cross-origin requests.
// Now reads Cors:AllowedOrigins from config (set per-environment in appsettings).
// Development falls back to localhost only if config is absent.
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>();

    if (allowedOrigins is null || allowedOrigins.Length == 0)
    {
        // Safe fallback: localhost only — never allow any origin in production
        allowedOrigins = builder.Environment.IsDevelopment()
            ? new[] { "http://localhost:3000", "https://localhost:3000", "http://localhost:5173", "https://localhost:5173" }
            : Array.Empty<string>();
    }

    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Strict policy for auth endpoint — per IP, 10 req/min
    options.AddFixedWindowLimiter("token-endpoint", o =>
    {
        o.PermitLimit = 10;
        o.Window      = TimeSpan.FromMinutes(1);
        o.QueueLimit  = 0;
    });

    // Default policy — per tenant (100 req/min), fallback to IP if no tenant header
    options.AddPolicy("tenant-fixed", httpContext =>
    {
        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        var partitionKey = !string.IsNullOrEmpty(tenantId)
            ? $"tenant:{tenantId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window      = TimeSpan.FromMinutes(1),
                QueueLimit  = 0
            });
    });

    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await context.HttpContext.Response.WriteAsync(
            "Rate limit exceeded. Please retry after 60 seconds.", cancellationToken);
    };
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion                   = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions                   = true;
    options.ApiVersionReader                    = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
});

var app = builder.Build();

app.UseGlobalExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiGateway.Middleware.ClaimsForwardingMiddleware>();

app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();
