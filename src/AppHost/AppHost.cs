using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka").WithKafkaUI();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("postgres-data");

var identityDb = postgres.AddDatabase("IdentityDb");

var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb).WithReference(kafka)
    .WaitFor(identityDb).WaitFor(kafka);

builder.Build().Run();




