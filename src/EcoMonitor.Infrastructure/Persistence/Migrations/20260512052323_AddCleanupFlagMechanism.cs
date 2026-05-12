using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCleanupFlagMechanism : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cleanup_flagged_at",
                table: "dumpsite_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cleanup_flagged_by_crew_id",
                table: "dumpsite_reports",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cleanup_rejection_notes",
                table: "dumpsite_reports",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "cleanup_rejection_reason",
                table: "dumpsite_reports",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reassign_count",
                table: "dumpsite_reports",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cleanup_flagged_at",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_flagged_by_crew_id",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_rejection_notes",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "cleanup_rejection_reason",
                table: "dumpsite_reports");

            migrationBuilder.DropColumn(
                name: "reassign_count",
                table: "dumpsite_reports");
        }
    }
}
