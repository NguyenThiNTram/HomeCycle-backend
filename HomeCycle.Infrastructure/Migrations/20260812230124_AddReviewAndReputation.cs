using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewAndReputation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "Review_OrderId_key",
                schema: "public",
                table: "Review");

            migrationBuilder.AddColumn<int>(
                name: "ReputationScore",
                schema: "public",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AlterColumn<int>(
                name: "DeliveryMethod",
                schema: "public",
                table: "Shipment",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerReadyAt",
                schema: "public",
                table: "Shipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequiredNote",
                schema: "public",
                table: "GHN_Shipment",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientOrderCode",
                schema: "public",
                table: "GHN_Shipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreationStatus",
                schema: "public",
                table: "GHN_Shipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDeliveryAt",
                schema: "public",
                table: "GHN_Shipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCreateAttemptAt",
                schema: "public",
                table: "GHN_Shipment",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastErrorCode",
                schema: "public",
                table: "GHN_Shipment",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "uq_review_order_reviewer",
                schema: "public",
                table: "Review",
                columns: new[] { "OrderId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_ghn_shipment_client_order_code",
                schema: "public",
                table: "GHN_Shipment",
                column: "ClientOrderCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_review_order_reviewer",
                schema: "public",
                table: "Review");

            migrationBuilder.DropIndex(
                name: "uq_ghn_shipment_client_order_code",
                schema: "public",
                table: "GHN_Shipment");

            migrationBuilder.DropColumn(
                name: "ReputationScore",
                schema: "public",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerReadyAt",
                schema: "public",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "CreationStatus",
                schema: "public",
                table: "GHN_Shipment");

            migrationBuilder.DropColumn(
                name: "ExpectedDeliveryAt",
                schema: "public",
                table: "GHN_Shipment");

            migrationBuilder.DropColumn(
                name: "LastCreateAttemptAt",
                schema: "public",
                table: "GHN_Shipment");

            migrationBuilder.DropColumn(
                name: "LastErrorCode",
                schema: "public",
                table: "GHN_Shipment");

            migrationBuilder.AlterColumn<int>(
                name: "DeliveryMethod",
                schema: "public",
                table: "Shipment",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "RequiredNote",
                schema: "public",
                table: "GHN_Shipment",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientOrderCode",
                schema: "public",
                table: "GHN_Shipment",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "Review_OrderId_key",
                schema: "public",
                table: "Review",
                column: "OrderId",
                unique: true);
        }
    }
}
