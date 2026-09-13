using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryAtelierBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FirstShipmentSentAt",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextShipmentEstimatedAt",
                table: "OrderItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstShipmentSentAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "NextShipmentEstimatedAt",
                table: "OrderItems");
        }
    }
}
