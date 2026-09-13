using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryAtelierBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemShippedNowQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShippedNowQuantity",
                table: "OrderItems",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippedNowQuantity",
                table: "OrderItems");
        }
    }
}
