using UMS.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SigningKey"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapStudentEndpoints();

app.Run();








