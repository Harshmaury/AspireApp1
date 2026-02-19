using Identity.API.Services;
using Identity.Application;
using Identity.Application.Interfaces;
using Identity.Infrastructure;
using Identity.Infrastructure.Persistence;
using Identity.API.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -- Services --------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

// -- Build -----------------------------------------------------
var app = builder.Build();

// -- Auto-migrate on startup (dev only) ------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// -- Endpoints -------------------------------------------------
app.MapAuthEndpoints();

app.Run();
