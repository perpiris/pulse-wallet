using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "LedgerEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceWalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWalletId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_Wallets_DestinationWalletId",
                        column: x => x.DestinationWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_Wallets_SourceWalletId",
                        column: x => x.SourceWalletId,
                        principalTable: "Wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TransferId",
                table: "LedgerEntries",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_DestinationWalletId",
                table: "Transfers",
                column: "DestinationWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_SourceWalletId_IdempotencyKey",
                table: "Transfers",
                columns: new[] { "SourceWalletId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_Transfers_TransferId",
                table: "LedgerEntries",
                column: "TransferId",
                principalTable: "Transfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_Transfers_TransferId",
                table: "LedgerEntries");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TransferId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "LedgerEntries");
        }
    }
}
