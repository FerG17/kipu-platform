using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddProductUnidadDeMedidaAndPresentacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "presentacion",
                table: "products",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "UNIDAD");

            migrationBuilder.AddColumn<string>(
                name: "unidad_de_medida",
                table: "products",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "UNIDAD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "presentacion",
                table: "products");

            migrationBuilder.DropColumn(
                name: "unidad_de_medida",
                table: "products");
        }
    }
}
