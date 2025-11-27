using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsApprovedToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isAproved",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isAproved",
                table: "Sellers");
        }
    }
}
