using Microsoft.AspNetCore.RateLimiting;
using UMS.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Threading.RateLimiting;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();

// Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = false;
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Strict policy for auth endpoint — per IP, 10 req/min
    options.AddFixedWindowLimiter("token-endpoint", o =>
    {
        o.PermitLimit   = 10;
        o.Window        = TimeSpan.FromMinutes(1);
        o.QueueLimit    = 0;
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

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ApiGateway.Middleware.ClaimsForwardingMiddleware>();

// Map versioned YARP proxy
app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
