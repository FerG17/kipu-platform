using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "password_reset_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    code_hash = table.Column<string>(type: "longtext", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "datetime", nullable: false),
                    attempt_count = table.Column<int>(type: "int", nullable: false),
                    is_verified = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_codes", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_codes_user_id",
                table: "password_reset_codes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "password_reset_codes");
        }
    }
}
