using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomeCycle.Infrastructure.Migrations;

[DbContext(typeof(HomeCycleDbContext))]
[Migration("20260915110000_AllowPostAndReviewDisputeCategories")]
public class AllowPostAndReviewDisputeCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE public."Dispute_Category_Target"
                DROP CONSTRAINT IF EXISTS ck_dispute_category_target_type;

            ALTER TABLE public."Dispute_Category_Target"
                ADD CONSTRAINT ck_dispute_category_target_type
                CHECK ("TargetType" IN (1, 2, 3, 4));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM public."Dispute_Category_Target"
                    WHERE "TargetType" IN (3, 4)
                ) THEN
                    RAISE EXCEPTION 'Remove Post and Review dispute category targets before reverting this migration.';
                END IF;
            END $$;

            ALTER TABLE public."Dispute_Category_Target"
                DROP CONSTRAINT ck_dispute_category_target_type;

            ALTER TABLE public."Dispute_Category_Target"
                ADD CONSTRAINT ck_dispute_category_target_type
                CHECK ("TargetType" IN (1, 2));
            """);
    }
}
