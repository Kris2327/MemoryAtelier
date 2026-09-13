using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoryAtelierBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCancellationNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationNote",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationNote",
                table: "Orders");
        }
    }
}
