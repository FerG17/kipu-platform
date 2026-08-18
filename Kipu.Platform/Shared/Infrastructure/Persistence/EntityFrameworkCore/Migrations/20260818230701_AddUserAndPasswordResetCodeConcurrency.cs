using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kipu.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndPasswordResetCodeConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // MySQL's DATETIME range starts at 1000-01-01 — the tool's own
            // DateTimeOffset.MinValue default (year 1) is outside that range
            // and MySQL rejects it outright under strict mode. A raw SQL
            // default (not `defaultValue:`) is required here too: Pomelo
            // generates a `DEFAULT TIMESTAMP '...'` literal for a typed
            // DateTimeOffset default against a `datetime` column, and MySQL
            // 8.4 rejects that exact syntax ("Invalid default value") even
            // though the same value works as a plain quoted string. Unix
            // epoch is a safe stand-in: any pre-existing row backfilled with
            // it is a password-reset code, which expires in 5 minutes anyway
            // (see PasswordResetCode.ResetCodeLifetime), so the exact
            // placeholder value has no real consequence.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "password_reset_codes",
                type: "datetime",
                nullable: false,
                defaultValueSql: "'1970-01-01 00:00:00'");

            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "password_reset_codes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "users");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "password_reset_codes");

            migrationBuilder.DropColumn(
                name: "version",
                table: "password_reset_codes");
        }
    }
}
