using UMS.SharedKernel.Extensions;
using Fee.Application;
using Fee.Infrastructure;
using Fee.API.Endpoints;
using Fee.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.AddSerilogDefaults();
builder.AddNpgsqlHealthCheck("FeeDb");
builder.Services.AddFeeApplication();
builder.Services.AddFeeInfrastructure(builder.Configuration);
var app = builder.Build();
app.UseSerilogDefaults();
app.UseGlobalExceptionHandler();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FeeDbContext>();
    db.Database.Migrate();
}
app.MapDefaultEndpoints();
app.MapFeeStructureEndpoints();
app.MapFeePaymentEndpoints();
app.MapScholarshipEndpoints();
app.Run();





