using Faculty.API.Endpoints;
using Faculty.API.Middleware;
using Faculty.API.Services;
using Faculty.Application;
using Faculty.Infrastructure;
using Faculty.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("FacultyDb");
builder.Services.AddOpenApi();
builder.Services.AddFacultyApplication();
builder.Services.AddFacultyInfrastructure(builder.Configuration);
builder.Services.AddHostedService<FacultyOutboxRelayService>();

builder.Services.AddHostedService<MigrationHostedService<FacultyDbContext>>();
var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.UseMiddleware<Faculty.API.Middleware.TenantMiddleware>();
app.UseHttpsRedirection();
app.MapFacultyEndpoints();
app.MapRegionHealthEndpoints();
app.Run();
