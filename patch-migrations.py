import os

MIGRATE_FN = """
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
"""

services = [
    (
        "src/Services/Academic/Academic.API/Program.cs",
        "Academic.Infrastructure.Persistence",
        "AcademicDbContext",
        "app.UseGlobalExceptionHandler();"
    ),
    (
        "src/Services/Attendance/Attendance.API/Program.cs",
        "Attendance.Infrastructure.Persistence",
        "AttendanceDbContext",
        "app.UseGlobalExceptionHandler();"
    ),
    (
        "src/Services/Faculty/Faculty.API/Program.cs",
        "Faculty.Infrastructure.Persistence",
        "FacultyDbContext",
        "app.UseGlobalExceptionHandler();"
    ),
    (
        "src/Services/Hostel/Hostel.API/Program.cs",
        "Hostel.Infrastructure.Persistence",
        "HostelDbContext",
        "app.UseGlobalExceptionHandler();"
    ),
]

MIGRATE_CALL = """await MigrateWithRetryAsync<{ctx}>(app.Services);
"""

for path, ns, ctx, anchor in services:
    content = open(path).read()

    # Add usings if not present
    for using in [f"using {ns};", "using Microsoft.EntityFrameworkCore;"]:
        if using not in content:
            content = using + "\n" + content

    # Add migrate call after anchor if not already present
    migrate_call = MIGRATE_CALL.format(ctx=ctx)
    if "MigrateWithRetryAsync" not in content:
        content = content.replace(
            anchor,
            anchor + "\n" + migrate_call
        )
        content += "\n" + MIGRATE_FN

    open(path, 'w').write(content)
    print(f"  patched {path}")

print("Done — 4 services patched with MigrateWithRetryAsync")