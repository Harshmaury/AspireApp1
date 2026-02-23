using UMS.SharedKernel.Extensions;
using Attendance.Application;
using Attendance.Infrastructure;
using Attendance.API.Endpoints;
using Attendance.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAttendanceApplication();
builder.Services.AddAttendanceInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Attendance.API.Middleware.TenantMiddleware>();
app.UseHttpsRedirection();
app.MapAttendanceEndpoints();
app.Run();



