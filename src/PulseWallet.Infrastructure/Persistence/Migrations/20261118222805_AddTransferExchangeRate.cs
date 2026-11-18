using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseWallet.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferExchangeRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "Transfers",
                type: "decimal(18,8)",
                precision: 18,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RateDate",
                table: "Transfers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceivedAmount",
                table: "Transfers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedCurrency",
                table: "Transfers",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            // Every transfer so far was in one currency: the receiver got exactly what the sender paid.
            migrationBuilder.Sql("UPDATE Transfers SET ReceivedAmount = Amount, ReceivedCurrency = Currency;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Rate",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "RateDate",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceivedAmount",
                table: "Transfers");

            migrationBuilder.DropColumn(
                name: "ReceivedCurrency",
                table: "Transfers");
        }
    }
}
