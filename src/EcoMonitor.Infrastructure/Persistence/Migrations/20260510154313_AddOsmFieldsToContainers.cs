using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOsmFieldsToContainers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_imported",
                table: "waste_containers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "osm_id",
                table: "waste_containers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_waste_containers_osm_id",
                table: "waste_containers",
                column: "osm_id",
                unique: true,
                filter: "osm_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_waste_containers_osm_id",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "is_imported",
                table: "waste_containers");

            migrationBuilder.DropColumn(
                name: "osm_id",
                table: "waste_containers");
        }
    }
}
