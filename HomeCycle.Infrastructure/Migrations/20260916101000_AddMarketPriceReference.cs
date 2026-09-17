using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomeCycle.Infrastructure.Migrations;

[DbContext(typeof(HomeCycleDbContext))]
[Migration("20260916101000_AddMarketPriceReference")]
public sealed class AddMarketPriceReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The manually created Supabase table is adopted without dropping its rows.
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS public."Market_Price_Reference" (
                "Id" uuid DEFAULT gen_random_uuid() NOT NULL,
                "ProductTypeId" uuid NOT NULL,
                "BrandId" uuid NULL,
                "ModelNumber" varchar(100) NOT NULL,
                "NormalizedModelNumber" varchar(100) NOT NULL,
                "ProductName" varchar(255) NULL,
                "KeySpecifications" jsonb NOT NULL,
                "HasVariants" boolean NOT NULL,
                "PriceVndPerUnit" numeric(18,2) NOT NULL,
                "SourceName" varchar(200) NOT NULL,
                "SourceUrl" varchar(1000) NOT NULL,
                "ObservedAt" timestamptz NOT NULL,
                "IsVerified" boolean NOT NULL,
                "CreatedAt" timestamptz NOT NULL,
                "UpdatedAt" timestamptz NOT NULL,
                CONSTRAINT "Market_Price_Reference_pkey" PRIMARY KEY ("Id")
            );
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'public'
                    AND table_name = 'Market_Price_Reference' AND column_name = 'id') THEN
                    ALTER TABLE public."Market_Price_Reference" RENAME COLUMN id TO "Id";
                END IF;
            END $$;
            -- Refuse invalid data rather than silently truncate strings or replace values.
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM public."Market_Price_Reference"
                    WHERE length("ModelNumber") > 100 OR length("NormalizedModelNumber") > 100
                       OR length("ProductName") > 255 OR length("SourceName") > 200 OR length("SourceUrl") > 1000) THEN
                    RAISE EXCEPTION 'Market reference text exceeds configured length; review rows before migration';
                END IF;
            END $$;
            ALTER TABLE public."Market_Price_Reference"
                ALTER COLUMN "ProductTypeId" DROP DEFAULT,
                ALTER COLUMN "BrandId" DROP DEFAULT,
                ALTER COLUMN "ModelNumber" TYPE varchar(100),
                ALTER COLUMN "NormalizedModelNumber" TYPE varchar(100),
                ALTER COLUMN "ProductName" TYPE varchar(255),
                ALTER COLUMN "ProductName" DROP NOT NULL,
                ALTER COLUMN "SourceName" TYPE varchar(200),
                ALTER COLUMN "SourceUrl" TYPE varchar(1000),
                ALTER COLUMN "KeySpecifications" TYPE jsonb USING "KeySpecifications"::jsonb,
                ALTER COLUMN "PriceVndPerUnit" TYPE numeric(18,2),
                ADD CONSTRAINT "CK_MarketPriceReference_Specifications" CHECK (jsonb_typeof("KeySpecifications") = 'object'),
                ADD CONSTRAINT "CK_MarketPriceReference_Price" CHECK ("PriceVndPerUnit" > 0);
            CREATE INDEX IF NOT EXISTS ix_market_price_verified_lookup
                ON public."Market_Price_Reference" ("ProductTypeId", "BrandId", "NormalizedModelNumber", "ObservedAt" DESC)
                WHERE "IsVerified" = true;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // This migration may adopt pre-existing manually maintained data. Never drop it on rollback.
        throw new NotSupportedException("Review a manual rollback for the adopted Market_Price_Reference table.");
    }
}
