using Aspire.Hosting;
var builder = DistributedApplication.CreateBuilder(args);
var cache = builder.AddRedis("cache");
var kafka = builder.AddKafka("kafka").WithKafkaUI();
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("postgres-data");
var identityDb      = postgres.AddDatabase("IdentityDb");
var studentDb       = postgres.AddDatabase("StudentDb");
var academicDb      = postgres.AddDatabase("AcademicDb");
var examinationDb   = postgres.AddDatabase("ExaminationDb");
var feeDb           = postgres.AddDatabase("FeeDb");
var notificationDb  = postgres.AddDatabase("NotificationDb");
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb).WithReference(kafka)
    .WaitFor(identityDb).WaitFor(kafka);
var apiService = builder.AddProject<Projects.AspireApp1_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi).WithReference(kafka)
    .WaitFor(identityApi);
var studentApi = builder.AddProject<Projects.Student_API>("student-api")
    .WithHttpHealthCheck("/health")
    .WithReference(studentDb).WithReference(kafka)
    .WaitFor(studentDb).WaitFor(kafka);
var academicApi = builder.AddProject<Projects.Academic_API>("academic-api")
    .WithHttpHealthCheck("/health")
    .WithReference(academicDb).WithReference(kafka)
    .WaitFor(academicDb).WaitFor(kafka);
var examinationApi = builder.AddProject<Projects.Examination_API>("examination-api")
    .WithHttpHealthCheck("/health")
    .WithReference(examinationDb).WithReference(kafka)
    .WaitFor(examinationDb).WaitFor(kafka);
var feeApi = builder.AddProject<Projects.Fee_API>("fee-api")
    .WithHttpHealthCheck("/health")
    .WithReference(feeDb).WithReference(kafka)
    .WaitFor(feeDb).WaitFor(kafka);
var notificationApi = builder.AddProject<Projects.Notification_API>("notification-api")
    .WithHttpHealthCheck("/health")
    .WithReference(notificationDb).WithReference(kafka)
    .WaitFor(notificationDb).WaitFor(kafka)
    .WaitFor(identityApi).WaitFor(studentApi)
    .WaitFor(academicApi).WaitFor(examinationApi).WaitFor(feeApi);
var apiGateway = builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithExternalHttpEndpoints()
    .WithReference(identityApi).WithReference(apiService)
    .WithReference(studentApi).WithReference(academicApi)
    .WithReference(examinationApi).WithReference(feeApi)
    .WaitFor(identityApi).WaitFor(apiService)
    .WaitFor(studentApi).WaitFor(academicApi)
    .WaitFor(examinationApi).WaitFor(feeApi);
builder.AddProject<Projects.AspireApp1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache).WaitFor(cache)
    .WithReference(apiGateway).WaitFor(apiGateway);
builder.Build().Run();
