using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomeCycle.Infrastructure.Migrations;

[DbContext(typeof(HomeCycleDbContext))]
[Migration("20260917100000_AllowLegacyReviewReputationDelta")]
public sealed class AllowLegacyReviewReputationDelta : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Adopt the existing nullable column, or add it on databases without it.
        // Preserve legacy NULLs: their historical impact cannot be reconstructed.
        migrationBuilder.Sql("""
            ALTER TABLE public."Review"
                ADD COLUMN IF NOT EXISTS "AppliedReputationDelta" integer;
            ALTER TABLE public."Review"
                ALTER COLUMN "AppliedReputationDelta" DROP NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "Review a manual rollback: legacy NULL deltas and existing reputation history must be preserved.");
    }
}
