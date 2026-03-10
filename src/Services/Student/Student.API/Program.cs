using AspireApp1.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Student.API.Endpoints;
using Student.API.Services;
using Student.Application;
using Student.Infrastructure;
using Student.Infrastructure.Persistence;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("StudentDb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddHostedService<StudentOutboxRelayService>();

// Run migration as a hosted service so app.Run() is not blocked
builder.Services.AddHostedService<MigrationHostedService<StudentDbContext>>();

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapStudentEndpoints();
app.MapRegionHealthEndpoints();

app.Run();

