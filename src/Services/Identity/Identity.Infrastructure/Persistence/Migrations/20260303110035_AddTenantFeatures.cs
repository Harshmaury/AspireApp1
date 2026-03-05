using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Features_AllowGuestAccess",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Features_AllowSelfRegistration",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Features_EnableAuditLog",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Features_EnableMfa",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Features_AllowGuestAccess",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Features_AllowSelfRegistration",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Features_EnableAuditLog",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Features_EnableMfa",
                table: "Tenants");
        }
    }
}
