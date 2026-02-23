using UMS.SharedKernel.Extensions;
using Academic.Application;
using Academic.Infrastructure;
using Academic.API.Endpoints;
using Academic.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("AcademicDb");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<Academic.API.Middleware.TenantMiddleware>();
app.MapDefaultEndpoints();

app.MapDepartmentEndpoints();
app.MapProgrammeEndpoints();
app.MapCourseEndpoints();
app.MapCurriculumEndpoints();
app.MapAcademicCalendarEndpoints();

app.Run();




