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

        var identityDb  = postgres.AddDatabase("IdentityDb");
        var studentDb   = postgres.AddDatabase("StudentDb");
        var academicDb  = postgres.AddDatabase("AcademicDb");

        var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
            .WithHttpHealthCheck("/health")
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

        var academicApi = builder.AddProject<Projects.Academic_API>("academic-api")
            .WithHttpHealthCheck("/health")
            .WithReference(academicDb)
            .WithReference(kafka)
            .WaitFor(academicDb)
            .WaitFor(kafka);

        var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
            .WithExternalHttpEndpoints()
            .WithReference(identityApi)
            .WithReference(apiService)
            .WithReference(studentApi)
            .WithReference(academicApi)
            .WaitFor(identityApi)
            .WaitFor(apiService)
            .WaitFor(studentApi)
            .WaitFor(academicApi);

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