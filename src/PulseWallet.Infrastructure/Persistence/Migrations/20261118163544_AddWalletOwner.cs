using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wallets created before sign-in existed have no owner, and there's no correct owner to give them.
            // Rather than invent one, fail with a clear message; with a non-null default of Guid.Empty the foreign key
            // below would fail anyway, just with a confusing constraint error. In development, reset the database.
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM Wallets)
                    THROW 50000, 'AddWalletOwner: existing wallets have no owner. Reset the development database (docker compose down -v) before applying this migration.', 1;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Wallets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_OwnerId",
                table: "Wallets",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_AspNetUsers_OwnerId",
                table: "Wallets",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_AspNetUsers_OwnerId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_OwnerId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Wallets");
        }
    }
}
