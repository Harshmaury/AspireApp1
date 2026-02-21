using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faculty.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Semester = table.Column<int>(type: "integer", nullable: false),
                    Section = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Faculty",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    Specialization = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HighestQualification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: false),
                    IsPhD = table.Column<bool>(type: "boolean", nullable: false),
                    JoiningDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculty", x => x.Id);
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
                name: "Publications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FacultyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Journal = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    PublishedYear = table.Column<int>(type: "integer", nullable: false),
                    DOI = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Type = table.Column<string>(type: "text", nullable: false),
                    CitationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Publications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_TenantId",
                table: "CourseAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseAssignments_TenantId_FacultyId_CourseId_AcademicYear",
                table: "CourseAssignments",
                columns: new[] { "TenantId", "FacultyId", "CourseId", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_TenantId",
                table: "Faculty",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_TenantId_EmployeeId",
                table: "Faculty",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Faculty_TenantId_UserId",
                table: "Faculty",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Publications_TenantId",
                table: "Publications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Publications_TenantId_FacultyId",
                table: "Publications",
                columns: new[] { "TenantId", "FacultyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseAssignments");

            migrationBuilder.DropTable(
                name: "Faculty");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "Publications");
        }
    }
}
