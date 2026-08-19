using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "idempotency_key",
                table: "sales",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);

            // Created before the old index is dropped: MySQL refuses to drop
            // ix_sales_business_id while it's the only index backing the
            // business_id foreign key, so the new composite index (which
            // covers that same leftmost column) has to exist first.
            migrationBuilder.CreateIndex(
                name: "ix_sales_business_id_idempotency_key",
                table: "sales",
                columns: new[] { "business_id", "idempotency_key" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "ix_sales_business_id",
                table: "sales");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Same ordering constraint as Up, in reverse: the old index has
            // to exist before the composite one backing the FK is dropped.
            migrationBuilder.CreateIndex(
                name: "ix_sales_business_id",
                table: "sales",
                column: "business_id");

            migrationBuilder.DropIndex(
                name: "ix_sales_business_id_idempotency_key",
                table: "sales");

            migrationBuilder.DropColumn(
                name: "idempotency_key",
                table: "sales");
        }
    }
}
