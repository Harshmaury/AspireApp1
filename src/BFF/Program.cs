using BFF.Endpoints;
using UMS.SharedKernel.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();

builder.Services.AddHttpClient("student-api",
    c => c.BaseAddress = new Uri("http://student-api"));
builder.Services.AddHttpClient("academic-api",
    c => c.BaseAddress = new Uri("http://academic-api"));
builder.Services.AddHttpClient("attendance-api",
    c => c.BaseAddress = new Uri("http://attendance-api"));
builder.Services.AddHttpClient("fee-api",
    c => c.BaseAddress = new Uri("http://fee-api"));

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
app.MapDefaultEndpoints();

app.MapDashboardEndpoints();
app.MapProfileEndpoints();

app.Run();
