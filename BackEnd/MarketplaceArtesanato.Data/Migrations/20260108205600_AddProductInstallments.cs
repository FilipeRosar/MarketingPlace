using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxInstallments",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxNoInterestInstallments",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxInstallments",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxNoInterestInstallments",
                table: "Products");
        }
    }
}
