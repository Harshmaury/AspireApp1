using AspireApp1.ServiceDefaults;
using Examination.API.Endpoints;
using Examination.Application;
using Examination.Infrastructure;
using Examination.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("ExaminationDb");
builder.Services.AddExaminationApplication();
builder.Services.AddExaminationInfrastructure(builder.Configuration);

// FIX PLAT-2: AddAuthentication was missing.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Auth:RequireHttpsMetadata", false);
    });
builder.Services.AddAuthorization();

builder.Services.AddHostedService<MigrationHostedService<ExaminationDbContext>>();
var app = builder.Build();

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapExamScheduleEndpoints();
app.MapMarksEntryEndpoints();
app.MapRegionHealthEndpoints();
app.Run();

