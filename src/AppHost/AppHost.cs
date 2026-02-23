using Aspire.Hosting;
var builder = DistributedApplication.CreateBuilder(args);
var kafka = builder.AddKafka("kafka").WithKafkaUI();
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume("postgres-data");
// Databases
var identityDb     = postgres.AddDatabase("IdentityDb");
var academicDb     = postgres.AddDatabase("AcademicDb");
var studentDb      = postgres.AddDatabase("StudentDb");
var attendanceDb   = postgres.AddDatabase("AttendanceDb");
var examinationDb  = postgres.AddDatabase("ExaminationDb");
var feeDb          = postgres.AddDatabase("FeeDb");
var notificationDb = postgres.AddDatabase("NotificationDb");
var facultyDb      = postgres.AddDatabase("FacultyDb");
var hostelDb       = postgres.AddDatabase("HostelDb");
// Services
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb).WithReference(kafka)
    .WaitFor(identityDb).WaitFor(kafka);
var academicApi = builder.AddProject<Projects.Academic_API>("academic-api")
    .WithHttpHealthCheck("/health")
    .WithReference(academicDb).WithReference(kafka)
    .WaitFor(academicDb).WaitFor(kafka);
var studentApi = builder.AddProject<Projects.Student_API>("student-api")
    .WithHttpHealthCheck("/health")
    .WithReference(studentDb).WithReference(kafka)
    .WaitFor(studentDb).WaitFor(kafka);
var attendanceApi = builder.AddProject<Projects.Attendance_API>("attendance-api")
    .WithHttpHealthCheck("/health")
    .WithReference(attendanceDb).WithReference(kafka)
    .WaitFor(attendanceDb).WaitFor(kafka);
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
    .WaitFor(notificationDb).WaitFor(kafka);
var facultyApi = builder.AddProject<Projects.Faculty_API>("faculty-api")
    .WithHttpHealthCheck("/health")
    .WithReference(facultyDb).WithReference(kafka)
    .WaitFor(facultyDb).WaitFor(kafka);
var hostelApi = builder.AddProject<Projects.Hostel_API>("hostel-api")
    .WithHttpHealthCheck("/health")
    .WithReference(hostelDb).WithReference(kafka)
    .WaitFor(hostelDb).WaitFor(kafka);
// API Gateway
builder.AddProject<Projects.ApiGateway>("api-gateway")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi)
    .WithReference(academicApi)
    .WithReference(studentApi)
    .WithReference(attendanceApi)
    .WithReference(examinationApi)
    .WithReference(feeApi)
    .WithReference(notificationApi)
    .WithReference(facultyApi)
    .WithReference(hostelApi)
    .WaitFor(identityApi);
// BFF
builder.AddProject<Projects.BFF>("bff")
    .WithHttpHealthCheck("/health")
    .WithReference(identityApi)
    .WithReference(studentApi)
    .WithReference(academicApi)
    .WithReference(attendanceApi)
    .WithReference(feeApi)
    .WaitFor(identityApi);

builder.Build().Run();
