using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchAndPurchaseOrderDetailLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "batch_label",
                table: "purchase_order_details",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "label",
                table: "batches",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "batch_label",
                table: "purchase_order_details");

            migrationBuilder.DropColumn(
                name: "label",
                table: "batches");
        }
    }
}
