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

// Identity
var identityApi = builder.AddProject<Projects.Identity_API>("identity-api")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb).WithReference(kafka)
    .WaitFor(identityDb).WaitFor(kafka);

// Academic
builder.AddProject<Projects.Academic_API>("academic-api")
    .WithHttpHealthCheck("/health")
    .WithReference(academicDb).WithReference(kafka)
    .WaitFor(academicDb).WaitFor(kafka);

// Student
builder.AddProject<Projects.Student_API>("student-api")
    .WithHttpHealthCheck("/health")
    .WithReference(studentDb).WithReference(kafka)
    .WaitFor(studentDb).WaitFor(kafka);

// Attendance
builder.AddProject<Projects.Attendance_API>("attendance-api")
    .WithHttpHealthCheck("/health")
    .WithReference(attendanceDb).WithReference(kafka)
    .WaitFor(attendanceDb).WaitFor(kafka);

// Examination
builder.AddProject<Projects.Examination_API>("examination-api")
    .WithHttpHealthCheck("/health")
    .WithReference(examinationDb).WithReference(kafka)
    .WaitFor(examinationDb).WaitFor(kafka);

// Fee
builder.AddProject<Projects.Fee_API>("fee-api")
    .WithHttpHealthCheck("/health")
    .WithReference(feeDb).WithReference(kafka)
    .WaitFor(feeDb).WaitFor(kafka);

// Notification
builder.AddProject<Projects.Notification_API>("notification-api")
    .WithHttpHealthCheck("/health")
    .WithReference(notificationDb).WithReference(kafka)
    .WaitFor(notificationDb).WaitFor(kafka);

// Faculty
builder.AddProject<Projects.Faculty_API>("faculty-api")
    .WithHttpHealthCheck("/health")
    .WithReference(facultyDb).WithReference(kafka)
    .WaitFor(facultyDb).WaitFor(kafka);

// Hostel
builder.AddProject<Projects.Hostel_API>("hostel-api")
    .WithHttpHealthCheck("/health")
    .WithReference(hostelDb).WithReference(kafka)
    .WaitFor(hostelDb).WaitFor(kafka);

builder.Build().Run();
