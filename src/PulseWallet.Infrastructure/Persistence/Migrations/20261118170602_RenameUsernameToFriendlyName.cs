using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWallet.Infrastructure.Persistence.Migrations
{
    // Hand-written: EF scaffolds a renamed property as DropColumn + AddColumn, which would erase every name
    // (and the new unique index would then fail for any user with two wallets, both named "").
    // A real rename keeps the data: existing usernames simply become the wallets' friendly names.
    public partial class RenameUsernameToFriendlyName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usernames were unique across all users; friendly names are unique per owner instead.
            migrationBuilder.DropIndex(
                name: "IX_Wallets_Username",
                table: "Wallets");

            // Replaced by the composite index below, which starts with OwnerId and so serves the foreign key too.
            migrationBuilder.DropIndex(
                name: "IX_Wallets_OwnerId",
                table: "Wallets");

            migrationBuilder.RenameColumn(
                name: "Username",
                table: "Wallets",
                newName: "FriendlyName");

            migrationBuilder.AlterColumn<string>(
                name: "FriendlyName",
                table: "Wallets",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_OwnerId_FriendlyName",
                table: "Wallets",
                columns: new[] { "OwnerId", "FriendlyName" },
                unique: true);
        }

        // Rolling back only works while every name still fits the old rules (20 characters, unique across all
        // users); otherwise shortening the column or recreating the global unique index fails and nothing changes.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Wallets_OwnerId_FriendlyName",
                table: "Wallets");

            migrationBuilder.AlterColumn<string>(
                name: "FriendlyName",
                table: "Wallets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40);

            migrationBuilder.RenameColumn(
                name: "FriendlyName",
                table: "Wallets",
                newName: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_OwnerId",
                table: "Wallets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_Username",
                table: "Wallets",
                column: "Username",
                unique: true);
        }
    }
}