using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerImageDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ImageHeight",
                table: "Banners",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageObjectFit",
                table: "Banners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageWidth",
                table: "Banners",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageHeight",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ImageObjectFit",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ImageWidth",
                table: "Banners");
        }
    }
}
