using Attendance.API.Endpoints;
using Attendance.Application;
using Attendance.Infrastructure;
using Attendance.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("AttendanceDb");
builder.Services.AddOpenApi();
builder.Services.AddAttendanceApplication();
builder.Services.AddAttendanceInfrastructure(builder.Configuration);

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

builder.Services.AddHostedService<MigrationHostedService<AttendanceDbContext>>();
var app = builder.Build();

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Attendance.API.Middleware.TenantMiddleware>();
app.UseHttpsRedirection();
app.MapAttendanceEndpoints();
app.MapRegionHealthEndpoints();
app.Run();
