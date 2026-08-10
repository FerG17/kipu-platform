using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bodega.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddReportFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "product_id",
                table: "reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "supplier_id",
                table: "reports",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "product_id",
                table: "reports");

            migrationBuilder.DropColumn(
                name: "supplier_id",
                table: "reports");
        }
    }
}
