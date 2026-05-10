using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAqiUsToReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "aqi_us",
                table: "air_quality_readings",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aqi_us",
                table: "air_quality_readings");
        }
    }
}
