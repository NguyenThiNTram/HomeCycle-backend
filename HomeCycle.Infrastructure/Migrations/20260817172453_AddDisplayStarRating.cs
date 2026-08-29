using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayStarRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                schema: "public",
                table: "Withdrawal",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessedBy",
                schema: "public",
                table: "Withdrawal",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                schema: "public",
                table: "Withdrawal",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "public",
                table: "Wallet",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Purpose",
                schema: "public",
                table: "Wallet",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DisplayStarRating",
                schema: "public",
                table: "Personal_Profile",
                type: "double precision",
                nullable: false,
                defaultValue: 4.7999999999999998);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                schema: "public",
                table: "Order",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderCode",
                schema: "public",
                table: "Order",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DisplayStarRating",
                schema: "public",
                table: "Business_Profile",
                type: "double precision",
                nullable: false,
                defaultValue: 4.7999999999999998);

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawal_ProcessedBy",
                schema: "public",
                table: "Withdrawal",
                column: "ProcessedBy");

            migrationBuilder.AddForeignKey(
                name: "fk_withdrawal_processedby",
                schema: "public",
                table: "Withdrawal",
                column: "ProcessedBy",
                principalSchema: "public",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_withdrawal_processedby",
                schema: "public",
                table: "Withdrawal");

            migrationBuilder.DropIndex(
                name: "IX_Withdrawal_ProcessedBy",
                schema: "public",
                table: "Withdrawal");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                schema: "public",
                table: "Withdrawal");

            migrationBuilder.DropColumn(
                name: "ProcessedBy",
                schema: "public",
                table: "Withdrawal");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                schema: "public",
                table: "Withdrawal");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "public",
                table: "Wallet");

            migrationBuilder.DropColumn(
                name: "DisplayStarRating",
                schema: "public",
                table: "Personal_Profile");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "public",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "OrderCode",
                schema: "public",
                table: "Order");

            migrationBuilder.DropColumn(
                name: "DisplayStarRating",
                schema: "public",
                table: "Business_Profile");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                schema: "public",
                table: "Wallet",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
