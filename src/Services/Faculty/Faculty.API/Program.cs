using UMS.SharedKernel.Extensions;
using Faculty.Application;
using Faculty.Infrastructure;
using Faculty.API.Endpoints;
using Faculty.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("FacultyDb");
builder.Services.AddOpenApi();
builder.Services.AddFacultyApplication();
builder.Services.AddFacultyInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapDefaultEndpoints();
app.UseMiddleware<Faculty.API.Middleware.TenantMiddleware>();
app.UseHttpsRedirection();
app.MapFacultyEndpoints();
app.Run();





