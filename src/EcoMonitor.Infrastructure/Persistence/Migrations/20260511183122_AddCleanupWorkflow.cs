using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cleanup_completed_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cleanup_crew_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cleanup_notes",
                table: "dumpsite_reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cleanup_started_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "inspector_observations",
                table: "dumpsite_reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "verified_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "verified_by_inspector_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "dumpsite_cleanup_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dumpsite_cleanup_photos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dumpsite_inspection_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    uploaded_by_inspector_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dumpsite_inspection_photos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_reports_cleanup_crew_id",
                table: "dumpsite_reports",
                column: "cleanup_crew_id");

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_cleanup_photos_report_id",
                table: "dumpsite_cleanup_photos",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_cleanup_photos_report_id_type",
                table: "dumpsite_cleanup_photos",
                columns: new[] { "report_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_dumpsite_inspection_photos_report_id",
                table: "dumpsite_inspection_photos",
                column: "report_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dumpsite_cleanup_photos");

            migrationBuilder.DropTable(
                name: "dumpsite_inspection_photos");

            migrationBuilder.DropIndex(
                name: "ix_dumpsite_reports_cleanup_crew_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_completed_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_crew_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_notes",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_started_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "inspector_observations",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "verified_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "verified_by_inspector_id",
                table: "dumpsite_reports");
        }
    }
}
