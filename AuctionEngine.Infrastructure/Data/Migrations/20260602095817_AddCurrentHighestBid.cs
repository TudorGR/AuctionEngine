using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuctionEngine.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentHighestBid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentHighestBid",
                table: "AuctionItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentHighestBid",
                table: "AuctionItems");
        }
    }
}
