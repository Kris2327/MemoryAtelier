using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryAtelierBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameSecondShipmentToFinalShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SecondShipmentSentAt",
                table: "OrderItems",
                newName: "FinalShipmentSentAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FinalShipmentSentAt",
                table: "OrderItems",
                newName: "SecondShipmentSentAt");
        }
    }
}
