using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    public partial class AddProductStory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "StoryEnabled",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StoryExperience",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryInspiration",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryMaker",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoryMarkdown",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductStoryMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStoryMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductStoryMedia_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductStoryMedia_ProductId",
                table: "ProductStoryMedia",
                column: "ProductId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductStoryMedia");

            migrationBuilder.DropColumn(
                name: "StoryEnabled",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoryExperience",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoryInspiration",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoryMaker",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StoryMarkdown",
                table: "Products");
        }
    }
}
