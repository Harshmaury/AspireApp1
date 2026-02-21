using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hostel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HostelComplaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostelComplaints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hostels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    TotalRooms = table.Column<int>(type: "integer", nullable: false),
                    WardenName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WardenContact = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hostels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                name: "RoomAllotments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostelId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BedNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AllottedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VacatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomAllotments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    HostelId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CurrentOccupancy = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HostelComplaints_TenantId_Status",
                table: "HostelComplaints",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HostelComplaints_TenantId_StudentId",
                table: "HostelComplaints",
                columns: new[] { "TenantId", "StudentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Hostels_TenantId",
                table: "Hostels",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                table: "OutboxMessages",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoomAllotments_TenantId_RoomId_AcademicYear",
                table: "RoomAllotments",
                columns: new[] { "TenantId", "RoomId", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomAllotments_TenantId_StudentId_AcademicYear",
                table: "RoomAllotments",
                columns: new[] { "TenantId", "StudentId", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_HostelId_RoomNumber",
                table: "Rooms",
                columns: new[] { "HostelId", "RoomNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_TenantId_HostelId",
                table: "Rooms",
                columns: new[] { "TenantId", "HostelId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostelComplaints");

            migrationBuilder.DropTable(
                name: "Hostels");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "RoomAllotments");

            migrationBuilder.DropTable(
                name: "Rooms");
        }
    }
}
