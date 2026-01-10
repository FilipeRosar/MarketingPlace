using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixUserFavoritesUserFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_Customer_UserId",
                table: "UserFavorites");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorites_Users_UserId",
                table: "UserFavorites",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserFavorites_Users_UserId",
                table: "UserFavorites");

            migrationBuilder.AddForeignKey(
                name: "FK_UserFavorites_Customer_UserId",
                table: "UserFavorites",
                column: "UserId",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
