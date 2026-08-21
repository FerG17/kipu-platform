using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchLotTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "batch_id",
                table: "stock_movements",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inventory_item_id",
                table: "batches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "batches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "remaining_quantity",
                table: "batches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // X5 Bloque C data backfill: batches never tracked their own
            // quantity before this migration — the closest available truth
            // is the InventoryItem each batch was already (loosely) linked
            // to. Prefer the old inventory_id when it points at a real row;
            // some existing batches have inventory_id = 0 (X5 #9's known
            // bug — a batch created together with a brand-new InventoryItem
            // in the same call, before EF had assigned its real id), so
            // those fall back to matching by (product_id, business_id).
            migrationBuilder.Sql(@"
                UPDATE batches b
                JOIN inventory_items ii ON ii.id = b.inventory_id
                SET b.inventory_item_id = ii.id,
                    b.quantity = ii.stock_unit,
                    b.remaining_quantity = ii.stock_unit;
            ");

            migrationBuilder.Sql(@"
                UPDATE batches b
                JOIN inventory_items ii ON ii.product_id = b.product_id AND ii.business_id = b.business_id
                SET b.inventory_item_id = ii.id,
                    b.quantity = ii.stock_unit,
                    b.remaining_quantity = ii.stock_unit
                WHERE b.inventory_item_id IS NULL;
            ");

            // A batch that still can't be matched to any InventoryItem (its
            // product has no stock record at all) never pointed at anything
            // real — nothing worth preserving.
            migrationBuilder.Sql("DELETE FROM batches WHERE inventory_item_id IS NULL;");

            migrationBuilder.DropColumn(
                name: "inventory_id",
                table: "batches");

            migrationBuilder.AlterColumn<int>(
                name: "inventory_item_id",
                table: "batches",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_movements_batch_id",
                table: "stock_movements",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_inventory_item_id",
                table: "batches",
                column: "inventory_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_batches_inventory_item_inventory_item_id",
                table: "batches",
                column: "inventory_item_id",
                principalTable: "inventory_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_stock_movements_batches_batch_id",
                table: "stock_movements",
                column: "batch_id",
                principalTable: "batches",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_batches_inventory_item_inventory_item_id",
                table: "batches");

            migrationBuilder.DropForeignKey(
                name: "fk_stock_movements_batches_batch_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_stock_movements_batch_id",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "ix_batches_inventory_item_id",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "batch_id",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "inventory_item_id",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "batches");

            migrationBuilder.DropColumn(
                name: "remaining_quantity",
                table: "batches");

            migrationBuilder.AddColumn<int>(
                name: "inventory_id",
                table: "batches",
                type: "int",
                nullable: true);
        }
    }
}
