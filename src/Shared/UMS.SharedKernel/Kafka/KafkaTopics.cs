// ============================================================
// UMS.SharedKernel — KafkaTopics
// Central registry of all Kafka topic names in UMS.
// ALWAYS reference these constants — never hardcode topic strings.
// ============================================================
namespace UMS.SharedKernel.Kafka;

/// <summary>
/// Central registry of all Kafka topic names used across UMS services.
/// Changing a topic name here propagates to all producers and consumers.
/// </summary>
public static class KafkaTopics
{
    public const string IdentityEvents     = "identity-events";
    public const string StudentEvents      = "student-events";
    public const string AcademicEvents     = "academic-events";
    public const string AttendanceEvents   = "attendance-events";
    public const string ExaminationEvents  = "examination-events";
    public const string FeeEvents          = "fee-events";
    public const string FacultyEvents      = "faculty-events";
    public const string HostelEvents       = "hostel-events";
    public const string NotificationEvents = "notification-events";
}
