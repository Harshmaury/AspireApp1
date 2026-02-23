using UMS.SharedKernel.Extensions;
using Microsoft.EntityFrameworkCore;
using Student.Application;
using Student.Infrastructure;
using Student.Infrastructure.Persistence;
using Student.API.Endpoints;
using Student.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("StudentDb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddHostedService<StudentOutboxRelayService>();

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
app.UseExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
    await db.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.MapStudentEndpoints();

app.Run();









