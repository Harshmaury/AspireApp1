using Microsoft.EntityFrameworkCore;
using Hostel.Infrastructure.Persistence;
using UMS.SharedKernel.Extensions;
using Hostel.API.Endpoints;
using Hostel.API.Middleware;
using Hostel.Application;
using Hostel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("HostelDb");


builder.Services.AddHostelApplication();
builder.Services.AddHostelInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
await MigrateWithRetryAsync<HostelDbContext>(app.Services);


app.MapDefaultEndpoints();
app.UseTenantMiddleware();

app.MapHostelEndpoints();
app.MapRoomEndpoints();
app.MapAllotmentEndpoints();
app.MapComplaintEndpoints();

app.Run();







static async Task MigrateWithRetryAsync<TDb>(IServiceProvider services,
    int maxRetries = 5, int delaySeconds = 3) where TDb : Microsoft.EntityFrameworkCore.DbContext
{
    using var scope = services.CreateScope();
    var db     = scope.ServiceProvider.GetRequiredService<TDb>();
    var logger = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TDb>>();
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
