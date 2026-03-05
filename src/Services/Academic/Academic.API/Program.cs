using Academic.API.Endpoints;
using Academic.Application;
using Academic.Infrastructure;
using Academic.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("AcademicDb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// FIX A1/PLAT-2: AddAuthentication was missing. TenantMiddleware reads ctx.User
// to extract TenantId — without this, ctx.User is always unauthenticated,
// TenantId is never set, and every protected endpoint throws UnauthorizedAccessException.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Auth:RequireHttpsMetadata", false);
    });
builder.Services.AddAuthorization();

builder.Services.AddHostedService<MigrationHostedService<AcademicDbContext>>();
var app = builder.Build();

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Academic.API.Middleware.TenantMiddleware>();

app.MapDepartmentEndpoints();
app.MapProgrammeEndpoints();
app.MapCourseEndpoints();
app.MapCurriculumEndpoints();
app.MapAcademicCalendarEndpoints();
app.MapRegionHealthEndpoints();
app.Run();
