using System;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    [DbContext(typeof(ArtesianDbContext))]
    [Migration("20260201090000_AddRatingSellerReply")]
    public partial class AddRatingSellerReply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerReply",
                table: "Ratings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerReplyAt",
                table: "Ratings",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerReply",
                table: "Ratings");

            migrationBuilder.DropColumn(
                name: "SellerReplyAt",
                table: "Ratings");
        }
    }
}
