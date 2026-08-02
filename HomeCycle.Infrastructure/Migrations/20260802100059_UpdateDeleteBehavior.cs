using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_post",
                schema: "public",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "fk_pav_attribute",
                schema: "public",
                table: "Product_Attribute_Value");

            migrationBuilder.DropForeignKey(
                name: "fk_pav_option",
                schema: "public",
                table: "Product_Attribute_Value");

            migrationBuilder.DropForeignKey(
                name: "fk_pav_product",
                schema: "public",
                table: "Product_Attribute_Value");

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

            migrationBuilder.AddForeignKey(
                name: "fk_product_post",
                schema: "public",
                table: "Product",
                column: "PostId",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "PostId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pav_attribute",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "AttributeId",
                principalSchema: "public",
                principalTable: "Product_Attribute",
                principalColumn: "AttributeId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_pav_option",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "OptionId",
                principalSchema: "public",
                principalTable: "Product_Attribute_Option",
                principalColumn: "OptionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_pav_product",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "ProductId",
                principalSchema: "public",
                principalTable: "Product",
                principalColumn: "ProductId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_product_post",
                schema: "public",
                table: "Product");

            migrationBuilder.DropForeignKey(
                name: "fk_pav_attribute",
                schema: "public",
                table: "Product_Attribute_Value");

            migrationBuilder.DropForeignKey(
                name: "fk_pav_option",
                schema: "public",
                table: "Product_Attribute_Value");

            migrationBuilder.DropForeignKey(
                name: "fk_pav_product",
                schema: "public",
                table: "Product_Attribute_Value");

            migrationBuilder.DropColumn(
                name: "InputMode",
                schema: "public",
                table: "Product_Attribute");

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

            migrationBuilder.AddForeignKey(
                name: "fk_product_post",
                schema: "public",
                table: "Product",
                column: "PostId",
                principalSchema: "public",
                principalTable: "Post",
                principalColumn: "PostId");

            migrationBuilder.AddForeignKey(
                name: "fk_pav_attribute",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "AttributeId",
                principalSchema: "public",
                principalTable: "Product_Attribute",
                principalColumn: "AttributeId");

            migrationBuilder.AddForeignKey(
                name: "fk_pav_option",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "OptionId",
                principalSchema: "public",
                principalTable: "Product_Attribute_Option",
                principalColumn: "OptionId");

            migrationBuilder.AddForeignKey(
                name: "fk_pav_product",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "ProductId",
                principalSchema: "public",
                principalTable: "Product",
                principalColumn: "ProductId");
        }
    }
}
