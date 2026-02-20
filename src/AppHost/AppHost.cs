public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        var cache = builder.AddRedis("cache");

        IResourceBuilder<KafkaServerResource> kafka = builder.AddKafka("kafka")
            .WithKafkaUI();

        var postgres = builder.AddPostgres("postgres")
            .WithPgAdmin()
            .WithDataVolume("postgres-data");

        var identityDb = postgres.AddDatabase("IdentityDb");
var studentDb = postgres.AddDatabase("StudentDb");

        var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
            .WithReference(identityDb)
            .WithReference(kafka)
            .WaitFor(identityDb)
            .WaitFor(kafka);

        var apiService = builder.AddProject<Projects.AspireApp1_ApiService>("apiservice")
            .WithHttpHealthCheck("/health")
            .WithReference(identityApi)
            .WithReference(kafka)
            .WaitFor(identityApi);

        var studentApi = builder.AddProject<Projects.Student_API>("student-api")
    .WithHttpHealthCheck("/health")
    .WithReference(studentDb)
    .WithReference(kafka)
    .WaitFor(studentDb)
    .WaitFor(kafka);

var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
            .WithExternalHttpEndpoints()
            .WithReference(identityApi)
            .WithReference(apiService)
            .WithReference(studentApi)
            .WaitFor(identityApi)
            .WaitFor(apiService)
            .WaitFor(studentApi);

        builder.AddProject<Projects.AspireApp1_Web>("webfrontend")
            .WithExternalHttpEndpoints()
            .WithHttpHealthCheck("/health")
            .WithReference(cache)
            .WaitFor(cache)
            .WithReference(apiGateway)
            .WaitFor(apiGateway);

        builder.Build().Run();
    }
}


