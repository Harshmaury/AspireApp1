using Microsoft.EntityFrameworkCore;
using Faculty.Infrastructure.Persistence;
using UMS.SharedKernel.Extensions;
using Faculty.Application;
using Faculty.Infrastructure;
using Faculty.API.Endpoints;
using Faculty.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("FacultyDb");
builder.Services.AddOpenApi();
builder.Services.AddFacultyApplication();
builder.Services.AddFacultyInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
await MigrateWithRetryAsync<FacultyDbContext>(app.Services);


if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.UseMiddleware<Faculty.API.Middleware.TenantMiddleware>();
app.UseHttpsRedirection();
app.MapFacultyEndpoints();
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
