using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixNullSellerIdInOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE OrderItems
                  SET SellerId = p.SellerId
                  FROM OrderItems oi
                  INNER JOIN Products p ON oi.ProductId = p.Id
                  WHERE (oi.SellerId = '00000000-0000-0000-0000-000000000000' OR oi.SellerId IS NULL)
                    AND p.SellerId IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This data migration is one-way - we can't reliably reverse it
        }
    }
}
