using Hostel.API.Endpoints;
using Hostel.API.Middleware;
using Hostel.Application;
using Hostel.Infrastructure;
using Hostel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("HostelDb");
builder.Services.AddHostelApplication();
builder.Services.AddHostelInfrastructure(builder.Configuration);

// FIX PLAT-2: AddAuthentication was missing. TenantMiddleware reads ctx.User
// to extract TenantId — without this, ctx.User is always unauthenticated,
// TenantId is never set, and every protected endpoint throws UnauthorizedAccessException.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Auth:RequireHttpsMetadata", false);
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
await MigrateWithRetryAsync<HostelDbContext>(app.Services);

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantMiddleware();
app.MapHostelEndpoints();
app.MapRoomEndpoints();
app.MapAllotmentEndpoints();
app.MapComplaintEndpoints();
app.MapRegionHealthEndpoints();
app.Run();

static async Task MigrateWithRetryAsync<TDb>(IServiceProvider services,
    int maxRetries = 5, int delaySeconds = 3) where TDb : DbContext
{
    using var scope = services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<TDb>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TDb>>();
    for (int i = 1; i <= maxRetries; i++)
    {
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("[Migration] {Db} succeeded on attempt {Attempt}", typeof(TDb).Name, i);
            return;
        }
        catch (Exception ex) when (i < maxRetries)
        {
            logger.LogWarning("[Migration] {Db} attempt {Attempt}/{Max} failed: {Msg}. Retrying in {Delay}s...",
                typeof(TDb).Name, i, maxRetries, ex.Message, delaySeconds);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }
    }
    await db.Database.MigrateAsync();
}
