using Fee.API.Endpoints;
using Fee.Application;
using Fee.Infrastructure;
using Fee.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("FeeDb");
builder.Services.AddFeeApplication();
builder.Services.AddFeeInfrastructure(builder.Configuration);

// FIX PLAT-2: AddAuthentication was missing.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience  = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Auth:RequireHttpsMetadata", false);
    });
builder.Services.AddAuthorization();

builder.Services.AddHostedService<MigrationHostedService<FeeDbContext>>();
var app = builder.Build();

app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapFeeStructureEndpoints();
app.MapFeePaymentEndpoints();
app.MapScholarshipEndpoints();
app.MapRegionHealthEndpoints();
app.Run();
