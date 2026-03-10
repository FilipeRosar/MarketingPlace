using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceArtesanato.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Coupons_IsActive_ExpiresAt",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "CouponUsages");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "MaxDiscountAmount",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "MaxTotalUses",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "MaxUsesPerUser",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "MinPurchaseAmount",
                table: "Coupons",
                newName: "MaxDiscount");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "CouponUsages",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CouponUsages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "PaidBy",
                table: "CouponUsages",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformPaid",
                table: "CouponUsages",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellerPaid",
                table: "CouponUsages",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "DiscountType",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Percentage");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Coupons",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<Guid>(
                name: "AutomationRuleId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorSellerId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinOrderValue",
                table: "Coupons",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "OnlyFirstPurchase",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnlyWithoutPromotion",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformSharePercentage",
                table: "Coupons",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreventsCombination",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerId",
                table: "Coupons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "UsageCount",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimit",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsageLimitPerUser",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Coupons",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidUntil",
                table: "Coupons",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CreatorSellerId",
                table: "Coupons",
                column: "CreatorSellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsActive_ValidUntil",
                table: "Coupons",
                columns: new[] { "IsActive", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Type",
                table: "Coupons",
                column: "Type");

            migrationBuilder.AddForeignKey(
                name: "FK_CouponUsages_Users_UserId",
                table: "CouponUsages",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CouponUsages_Users_UserId",
                table: "CouponUsages");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_CreatorSellerId",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_IsActive_ValidUntil",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_Type",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "PaidBy",
                table: "CouponUsages");

            migrationBuilder.DropColumn(
                name: "PlatformPaid",
                table: "CouponUsages");

            migrationBuilder.DropColumn(
                name: "SellerPaid",
                table: "CouponUsages");

            migrationBuilder.DropColumn(
                name: "AutomationRuleId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "CreatorSellerId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "MinOrderValue",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "OnlyFirstPurchase",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "OnlyWithoutPromotion",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "PlatformSharePercentage",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "PreventsCombination",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "UsageCount",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "UsageLimit",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "UsageLimitPerUser",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "ValidUntil",
                table: "Coupons");

            migrationBuilder.RenameColumn(
                name: "MaxDiscount",
                table: "Coupons",
                newName: "MinPurchaseAmount");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "CouponUsages",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CouponUsages",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "CouponUsages",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Coupons",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "DiscountType",
                table: "Coupons",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Percentage",
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Coupons",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Coupons",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDiscountAmount",
                table: "Coupons",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxTotalUses",
                table: "Coupons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsesPerUser",
                table: "Coupons",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_IsActive_ExpiresAt",
                table: "Coupons",
                columns: new[] { "IsActive", "ExpiresAt" });
        }
    }
}
