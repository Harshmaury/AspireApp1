using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Examination.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExamSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    ExamType = table.Column<string>(type: "text", nullable: false),
                    ExamDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Venue = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MaxMarks = table.Column<int>(type: "integer", nullable: false),
                    PassingMarks = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HallTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RollNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SeatNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsEligible = table.Column<bool>(type: "boolean", nullable: false),
                    IneligibilityReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HallTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarksEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamScheduleId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarksObtained = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Grade = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    GradePoint = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    IsAbsent = table.Column<bool>(type: "boolean", nullable: false),
                    EnteredBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarksEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResultCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    SGPA = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    CGPA = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    TotalCreditsEarned = table.Column<int>(type: "integer", nullable: false),
                    TotalCreditsAttempted = table.Column<int>(type: "integer", nullable: false),
                    HasBacklog = table.Column<bool>(type: "boolean", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultCards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedules_TenantId",
                table: "ExamSchedules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSchedules_TenantId_CourseId",
                table: "ExamSchedules",
                columns: new[] { "TenantId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_HallTickets_TenantId",
                table: "HallTickets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HallTickets_TenantId_StudentId_ExamScheduleId",
                table: "HallTickets",
                columns: new[] { "TenantId", "StudentId", "ExamScheduleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarksEntries_TenantId",
                table: "MarksEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MarksEntries_TenantId_StudentId_ExamScheduleId",
                table: "MarksEntries",
                columns: new[] { "TenantId", "StudentId", "ExamScheduleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResultCards_TenantId",
                table: "ResultCards",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultCards_TenantId_StudentId_AcademicYear_Semester",
                table: "ResultCards",
                columns: new[] { "TenantId", "StudentId", "AcademicYear", "Semester" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamSchedules");

            migrationBuilder.DropTable(
                name: "HallTickets");

            migrationBuilder.DropTable(
                name: "MarksEntries");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ResultCards");
        }
    }
}
