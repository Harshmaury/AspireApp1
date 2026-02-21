using Examination.Application;
using Examination.Infrastructure;
using Examination.API.Endpoints;
using Examination.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddExaminationApplication();
builder.Services.AddExaminationInfrastructure(builder.Configuration);
builder.Services.AddAuthentication("Bearer").AddJwtBearer();
builder.Services.AddAuthorization();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExaminationDbContext>();
    db.Database.Migrate();
}
app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapExamScheduleEndpoints();
app.MapMarksEntryEndpoints();
app.Run();
