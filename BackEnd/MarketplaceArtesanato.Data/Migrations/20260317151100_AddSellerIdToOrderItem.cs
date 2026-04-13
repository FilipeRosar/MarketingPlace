using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerIdToOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SellerId column to OrderItem table
            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "OrderItem",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            // Populate SellerId from Product table (backfill existing data)
            migrationBuilder.Sql(
                @"UPDATE oi SET oi.SellerId = p.SellerId 
                  FROM OrderItem oi 
                  INNER JOIN Product p ON oi.ProductId = p.Id");

            // Add indices for analytics performance
            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId",
                table: "OrderItem",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId_ProductId",
                table: "OrderItem",
                columns: new[] { "SellerId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_SellerId_CreatedAt",
                table: "OrderItem",
                columns: new[] { "SellerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_SellerId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_SellerId_ProductId",
                table: "OrderItem");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_SellerId_CreatedAt",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "OrderItem");
        }
    }
}
