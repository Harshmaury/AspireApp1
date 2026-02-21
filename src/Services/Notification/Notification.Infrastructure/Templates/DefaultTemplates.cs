using Notification.Domain.Entities;
using Notification.Domain.Enums;
namespace Notification.Infrastructure.Templates;
public static class DefaultTemplates
{
    public static List<NotificationTemplate> GetAll() => new()
    {
        NotificationTemplate.Create(Guid.Empty, "UserRegisteredEvent", NotificationChannel.Email,
            "Welcome to UMS, {{FirstName}}!",
            "<h2>Welcome, {{FirstName}} {{LastName}}!</h2><p>Your account has been created successfully.</p><p>Role: <strong>{{Role}}</strong></p><p>Email: {{Email}}</p>"),

        NotificationTemplate.Create(Guid.Empty, "StudentEnrolledEvent", NotificationChannel.Email,
            "Enrollment Confirmed — {{StudentNumber}}",
            "<h2>Dear {{FirstName}},</h2><p>Your enrollment has been confirmed.</p><p>Student Number: <strong>{{StudentNumber}}</strong></p>"),

        NotificationTemplate.Create(Guid.Empty, "ResultDeclaredEvent", NotificationChannel.Email,
            "Your Result for Semester {{Semester}} is Published",
            "<h2>Result Declared</h2><p>Academic Year: {{AcademicYear}} | Semester: {{Semester}}</p><p>SGPA: <strong>{{SGPA}}</strong> | CGPA: <strong>{{CGPA}}</strong></p>"),

        NotificationTemplate.Create(Guid.Empty, "FeePaymentReceivedEvent", NotificationChannel.Email,
            "Fee Payment Receipt — ?{{AmountPaid}}",
            "<h2>Payment Received</h2><p>Amount Paid: <strong>?{{AmountPaid}}</strong></p><p>Transaction ID: {{PaymentId}}</p>"),

        NotificationTemplate.Create(Guid.Empty, "FeeDefaulterMarkedEvent", NotificationChannel.Email,
            "Fee Due Reminder — {{AcademicYear}}",
            "<h2>Fee Due</h2><p>Your fee for academic year <strong>{{AcademicYear}}</strong> is pending.</p><p>Please pay immediately to avoid penalties.</p>"),

        NotificationTemplate.Create(Guid.Empty, "ScholarshipGrantedEvent", NotificationChannel.Email,
            "Scholarship Granted — ?{{Amount}}",
            "<h2>Congratulations!</h2><p>You have been awarded a scholarship of <strong>?{{Amount}}</strong>.</p>"),

        NotificationTemplate.Create(Guid.Empty, "StudentBacklogEvent", NotificationChannel.Email,
            "Backlog Alert — Immediate Action Required",
            "<h2>Backlog Notification</h2><p>You have a backlog in one or more courses. Please contact your academic advisor immediately.</p>"),

        NotificationTemplate.Create(Guid.Empty, "AcademicCalendarPublishedEvent", NotificationChannel.Email,
            "Academic Calendar Published — {{AcademicYear}} Semester {{Semester}}",
            "<h2>Academic Calendar</h2><p>Semester {{Semester}} of {{AcademicYear}} starts on <strong>{{StartDate}}</strong> and ends on <strong>{{EndDate}}</strong>.</p>")
    };
}
