namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Central registry of all Kafka topic names used across UMS services.
/// Always reference these constants instead of hardcoding topic strings.
/// </summary>
public static class KafkaTopics
{
    public const string StudentEvents     = "student-events";
    public const string FacultyEvents     = "faculty-events";
    public const string AcademicEvents    = "academic-events";
    public const string FeeEvents         = "fee-events";
    public const string IdentityEvents    = "identity-events";
    public const string AttendanceEvents  = "attendance-events";
    public const string ExaminationEvents = "examination-events";
    public const string HostelEvents      = "hostel-events";
    public const string NotificationEvents = "notification-events";
}