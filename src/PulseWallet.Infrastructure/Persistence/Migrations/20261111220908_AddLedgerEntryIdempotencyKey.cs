using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerEntryIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                table: "LedgerEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_WalletId_IdempotencyKey",
                table: "LedgerEntries",
                columns: new[] { "WalletId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_WalletId_IdempotencyKey",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "LedgerEntries");
        }
    }
}
