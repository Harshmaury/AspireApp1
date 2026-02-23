using UMS.SharedKernel.Extensions;
using Hostel.API.Endpoints;
using Hostel.API.Middleware;
using Hostel.Application;
using Hostel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("HostelDb");


builder.Services.AddHostelApplication();
builder.Services.AddHostelInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();

app.MapDefaultEndpoints();
app.UseTenantMiddleware();

app.MapHostelEndpoints();
app.MapRoomEndpoints();
app.MapAllotmentEndpoints();
app.MapComplaintEndpoints();

app.Run();





