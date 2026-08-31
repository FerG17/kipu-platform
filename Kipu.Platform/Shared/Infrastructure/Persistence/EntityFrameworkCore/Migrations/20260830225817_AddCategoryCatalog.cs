using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    business_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.ForeignKey(
                        name: "fk_categories_businesses_business_id",
                        column: x => x.business_id,
                        principalTable: "businesses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_categories_business_id_name",
                table: "categories",
                columns: new[] { "business_id", "name" },
                unique: true);

            // Backfill (X6 #5): every business that existed before this
            // migration needs a usable catalog too, not just businesses
            // signing up from now on (those get seeded by IAM's sign-up flow
            // via IProductContextFacade.SeedDefaultCategories). Two passes,
            // same idiom as AddBatchLotTracking's backfill: the first covers
            // the common case (the fixed vocabulary every business starts
            // with), the second mops up any custom "Otros" label a business
            // already typed into a product, so no product's existing
            // category value goes missing from its own dropdown.
            migrationBuilder.Sql(@"
                INSERT INTO categories (business_id, name)
                SELECT b.id, v.name
                FROM businesses b
                CROSS JOIN (
                    SELECT 'DAIRY' AS name UNION ALL SELECT 'GRAINS' UNION ALL SELECT 'OILS' UNION ALL
                    SELECT 'BEVERAGES' UNION ALL SELECT 'CLEANING' UNION ALL SELECT 'MEDICINE' UNION ALL SELECT 'OTHER'
                ) v;
            ");
            migrationBuilder.Sql(@"
                INSERT INTO categories (business_id, name)
                SELECT DISTINCT p.business_id, p.category
                FROM products p
                LEFT JOIN categories c ON c.business_id = p.business_id AND c.name = p.category
                WHERE c.id IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
