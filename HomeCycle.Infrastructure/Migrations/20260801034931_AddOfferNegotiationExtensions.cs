using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferNegotiationExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InputMode",
                schema: "public",
                table: "Product_Attribute",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SpaceUsage",
                schema: "public",
                table: "Product",
                type: "integer",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PriorityLevel",
                schema: "public",
                table: "Post",
                type: "integer",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FinalQuantity",
                schema: "public",
                table: "Negotiation",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BasePriceSnapshot",
                schema: "public",
                table: "Messages",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputMode",
                schema: "public",
                table: "Product_Attribute");

            migrationBuilder.DropColumn(
                name: "FinalQuantity",
                schema: "public",
                table: "Negotiation");

            migrationBuilder.DropColumn(
                name: "BasePriceSnapshot",
                schema: "public",
                table: "Messages");

            migrationBuilder.AlterColumn<string>(
                name: "SpaceUsage",
                schema: "public",
                table: "Product",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PriorityLevel",
                schema: "public",
                table: "Post",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
