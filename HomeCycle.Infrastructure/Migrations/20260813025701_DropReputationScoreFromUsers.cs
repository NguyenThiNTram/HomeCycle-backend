using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropReputationScoreFromUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReputationScore",
                schema: "public",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReputationScore",
                schema: "public",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 50);
        }
    }
}
