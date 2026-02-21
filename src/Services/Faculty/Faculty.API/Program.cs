using Faculty.Application;
using Faculty.Infrastructure;
using Faculty.API.Endpoints;
using Faculty.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddFacultyApplication();
builder.Services.AddFacultyInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<Faculty.API.Middleware.TenantMiddleware>();
app.UseHttpsRedirection();
app.MapFacultyEndpoints();
app.Run();
