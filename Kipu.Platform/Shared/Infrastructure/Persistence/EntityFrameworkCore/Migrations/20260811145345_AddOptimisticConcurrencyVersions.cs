using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimisticConcurrencyVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "purchase_orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "payment_plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "inventory_items",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "version",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "version",
                table: "payment_plans");

            migrationBuilder.DropColumn(
                name: "version",
                table: "inventory_items");
        }
    }
}
