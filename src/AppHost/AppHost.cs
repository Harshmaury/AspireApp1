var builder = DistributedApplication.CreateBuilder(args);

// -- Infrastructure --------------------------------------------
var cache = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();  // Gives you a web UI to inspect the DB

var identityDb = postgres.AddDatabase("IdentityDb");

// -- Services --------------------------------------------------
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithReference(identityDb)
    .WaitFor(identityDb);

var apiService = builder.AddProject<Projects.AspireApp1_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi)
    .WaitFor(identityApi);

// -- Frontend --------------------------------------------------
builder.AddProject<Projects.AspireApp1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
