using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// -- Observability ------------------------------------------------------------
var seq = builder.AddSeq("seq")
                 .ExcludeFromManifest();

var jaeger = builder.AddContainer("jaeger", "jaegertracing/all-in-one", "1.57")
                    .WithEndpoint(port: 16686, targetPort: 16686, name: "ui")
                    .WithEndpoint(port: 4317,  targetPort: 4317,  name: "otlp-grpc")
                    .ExcludeFromManifest();

var jaegerOtlp = jaeger.GetEndpoint("otlp-grpc");
var seqUrl     = seq.GetEndpoint("http");

// -- Infrastructure -----------------------------------------------------------
var kafka = builder.AddKafka("kafka").WithKafkaUI();

var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("postgres-data");

// -- Databases ----------------------------------------------------------------
var identityDb     = postgres.AddDatabase("IdentityDb");
var academicDb     = postgres.AddDatabase("AcademicDb");
var studentDb      = postgres.AddDatabase("StudentDb");
var attendanceDb   = postgres.AddDatabase("AttendanceDb");
var examinationDb  = postgres.AddDatabase("ExaminationDb");
var feeDb          = postgres.AddDatabase("FeeDb");
var notificationDb = postgres.AddDatabase("NotificationDb");
var facultyDb      = postgres.AddDatabase("FacultyDb");
var hostelDb       = postgres.AddDatabase("HostelDb");

// -- Services -----------------------------------------------------------------
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb).WithReference(kafka).WithReference(seq)
    .WaitFor(identityDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var academicApi = builder.AddProject<Projects.Academic_API>("academic-api")
    .WithHttpHealthCheck("/health")
    .WithReference(academicDb).WithReference(kafka).WithReference(seq)
    .WaitFor(academicDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var studentApi = builder.AddProject<Projects.Student_API>("student-api")
    .WithHttpHealthCheck("/health")
    .WithReference(studentDb).WithReference(kafka).WithReference(seq)
    .WaitFor(studentDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var attendanceApi = builder.AddProject<Projects.Attendance_API>("attendance-api")
    .WithHttpHealthCheck("/health")
    .WithReference(attendanceDb).WithReference(kafka).WithReference(seq)
    .WaitFor(attendanceDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var examinationApi = builder.AddProject<Projects.Examination_API>("examination-api")
    .WithHttpHealthCheck("/health")
    .WithReference(examinationDb).WithReference(kafka).WithReference(seq)
    .WaitFor(examinationDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var feeApi = builder.AddProject<Projects.Fee_API>("fee-api")
    .WithHttpHealthCheck("/health")
    .WithReference(feeDb).WithReference(kafka).WithReference(seq)
    .WaitFor(feeDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var notificationApi = builder.AddProject<Projects.Notification_API>("notification-api")
    .WithHttpHealthCheck("/health")
    .WithReference(notificationDb).WithReference(kafka).WithReference(seq)
    .WaitFor(notificationDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var facultyApi = builder.AddProject<Projects.Faculty_API>("faculty-api")
    .WithHttpHealthCheck("/health")
    .WithReference(facultyDb).WithReference(kafka).WithReference(seq)
    .WaitFor(facultyDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

var hostelApi = builder.AddProject<Projects.Hostel_API>("hostel-api")
    .WithHttpHealthCheck("/health")
    .WithReference(hostelDb).WithReference(kafka).WithReference(seq)
    .WaitFor(hostelDb).WaitFor(kafka)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

// -- API Gateway ---------------------------------------------------------------
builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi).WithReference(academicApi)
    .WithReference(studentApi).WithReference(attendanceApi)
    .WithReference(examinationApi).WithReference(feeApi)
    .WithReference(notificationApi).WithReference(facultyApi)
    .WithReference(hostelApi).WithReference(seq)
    .WaitFor(identityApi)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

// -- BFF -----------------------------------------------------------------------
builder.AddProject<Projects.BFF>("bff")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi).WithReference(studentApi)
    .WithReference(academicApi).WithReference(attendanceApi)
    .WithReference(feeApi).WithReference(seq)
    .WaitFor(identityApi)
    .WithEnvironment("Seq__ServerUrl", seqUrl)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", jaegerOtlp);

builder.Build().Run();