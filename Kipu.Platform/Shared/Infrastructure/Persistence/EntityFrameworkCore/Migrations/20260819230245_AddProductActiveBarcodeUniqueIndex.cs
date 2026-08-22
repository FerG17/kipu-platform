using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <summary>
    ///     X3 minor item: barcode uniqueness (ProductCommandService) was
    ///     read-then-write only — no DB-level guarantee, so two concurrent
    ///     creates with the same barcode could both pass the check and land.
    ///
    ///     This is deliberately NOT expressed via the EF model/Fluent API:
    ///     the existing rule is "unique among ACTIVE products only" —
    ///     deactivating a product frees its barcode for reuse
    ///     (ProductRepository.FindByBarcodeAsync already filters to
    ///     Status == Active). A plain `UNIQUE INDEX (business_id, barcode)`
    ///     would silently break that and block reusing a deactivated
    ///     product's old barcode. Instead: a STORED generated column that is
    ///     only non-null while the product is ACTIVE, with the unique index
    ///     on that — two ACTIVE products can never collide, but a
    ///     deactivated one's `active_barcode` becomes NULL (and MySQL treats
    ///     every NULL as distinct in a unique index, same fact already
    ///     relied on for the sale idempotency-key index), so its barcode is
    ///     free again exactly like today.
    ///
    ///     Not verified against production data — if two ACTIVE products in
    ///     the same business already share a non-null barcode, this
    ///     migration's CREATE UNIQUE INDEX will fail to apply. Check for
    ///     that before deploying.
    /// </summary>
    public partial class AddProductActiveBarcodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE `products` ADD COLUMN `active_barcode` VARCHAR(64) " +
                "GENERATED ALWAYS AS (CASE WHEN `status` = 'ACTIVE' THEN `barcode` END) STORED;");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX `ix_products_business_id_active_barcode` " +
                "ON `products` (`business_id`, `active_barcode`);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX `ix_products_business_id_active_barcode` ON `products`;");
            migrationBuilder.Sql("ALTER TABLE `products` DROP COLUMN `active_barcode`;");
        }
    }
}
