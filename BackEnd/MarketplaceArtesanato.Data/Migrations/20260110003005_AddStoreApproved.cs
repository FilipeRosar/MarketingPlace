using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreApproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StoreApproved",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreApproved",
                table: "Sellers");
        }
    }
}
