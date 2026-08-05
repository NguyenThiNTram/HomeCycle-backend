using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePendingOfferIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Chống tạo trùng Offer Pending cho cùng (Post, Sender) khi 2 request đồng thời
            // vượt qua ExistsPendingByPostAndSenderAsync(). Chỉ áp dụng khi OfferStatus = Pending (0).
            migrationBuilder.CreateIndex(
                name: "uq_offer_pending_post_sender",
                schema: "public",
                table: "Offer",
                columns: new[] { "PostId", "SenderId" },
                unique: true,
                filter: "\"OfferStatus\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_offer_pending_post_sender",
                schema: "public",
                table: "Offer");
        }
    }
}
