using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomeCycle.Infrastructure.Migrations;

[DbContext(typeof(HomeCycleDbContext))]
[Migration("20260916100000_AddPriceSuggestionDailyUsage")]
public sealed class AddPriceSuggestionDailyUsage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE public."PriceSuggestionDailyUsage" (
                "UserId" uuid NOT NULL,
                "UsageDate" date NOT NULL,
                "UsageCount" integer NOT NULL,
                CONSTRAINT "PK_PriceSuggestionDailyUsage" PRIMARY KEY ("UserId", "UsageDate"),
                CONSTRAINT "CK_PriceSuggestionDailyUsage_Count" CHECK ("UsageCount" BETWEEN 1 AND 5)
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PriceSuggestionDailyUsage", schema: "public");
    }
}

