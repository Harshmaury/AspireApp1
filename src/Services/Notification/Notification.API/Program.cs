using Microsoft.EntityFrameworkCore;
using Notification.API.Endpoints;
using Notification.Application;
using Notification.Infrastructure;
using Notification.Infrastructure.Persistence;
using UMS.SharedKernel.Extensions;
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("NotificationDb");
builder.Services.AddNotificationApplication();
builder.Services.AddNotificationInfrastructure(builder.Configuration);
var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
await Notification.Infrastructure.DependencyInjection.SeedDefaultTemplatesAsync(app.Services);
app.MapDefaultEndpoints();
app.MapRegionHealthEndpoints();
app.Run();




