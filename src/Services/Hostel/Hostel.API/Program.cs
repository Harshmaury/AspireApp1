using Hostel.API.Endpoints;
using Hostel.API.Middleware;
using Hostel.Application;
using Hostel.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Auth:Authority"];
        opts.Audience = builder.Configuration["Auth:Audience"];
        opts.RequireHttpsMetadata = false;
    });
builder.Services.AddAuthorization();

builder.Services.AddHostelApplication();
builder.Services.AddHostelInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenantMiddleware();

app.MapHostelEndpoints();
app.MapRoomEndpoints();
app.MapAllotmentEndpoints();
app.MapComplaintEndpoints();

app.Run();
