using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeCycle.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Clean_Project : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth.aal_level", "aal1,aal2,aal3")
                .Annotation("Npgsql:Enum:auth.code_challenge_method", "s256,plain")
                .Annotation("Npgsql:Enum:auth.factor_status", "unverified,verified")
                .Annotation("Npgsql:Enum:auth.factor_type", "totp,webauthn,phone")
                .Annotation("Npgsql:Enum:auth.oauth_authorization_status", "pending,approved,denied,expired")
                .Annotation("Npgsql:Enum:auth.oauth_client_type", "public,confidential")
                .Annotation("Npgsql:Enum:auth.oauth_registration_type", "dynamic,manual")
                .Annotation("Npgsql:Enum:auth.oauth_response_type", "code")
                .Annotation("Npgsql:Enum:auth.one_time_token_type", "confirmation_token,reauthentication_token,recovery_token,email_change_token_new,email_change_token_current,phone_change_token")
                .Annotation("Npgsql:Enum:realtime.action", "INSERT,UPDATE,DELETE,TRUNCATE,ERROR")
                .Annotation("Npgsql:Enum:realtime.equality_op", "eq,neq,lt,lte,gt,gte,in,like,ilike,is,match,imatch,isdistinct")
                .Annotation("Npgsql:Enum:storage.buckettype", "STANDARD,ANALYTICS,VECTOR")
                .Annotation("Npgsql:PostgresExtension:extensions.pg_stat_statements", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.pgcrypto", ",,")
                .Annotation("Npgsql:PostgresExtension:extensions.uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vault.supabase_vault", ",,");

            migrationBuilder.CreateTable(
                name: "Brand",
                schema: "public",
                columns: table => new
                {
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brand", x => x.BrandId);
                });

            migrationBuilder.CreateTable(
                name: "Category",
                schema: "public",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Category_pkey", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Media",
                schema: "public",
                columns: table => new
                {
                    MediaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Media_pkey", x => x.MediaId);
                });

            migrationBuilder.CreateTable(
                name: "Platform_Policy",
                schema: "public",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Platform_Policy_pkey", x => x.PolicyId);
                });

            migrationBuilder.CreateTable(
                name: "Post",
                schema: "public",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "integer", nullable: false),
                    PostType = table.Column<int>(type: "integer", nullable: true),
                    BasePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    StreetAddress = table.Column<string>(type: "text", nullable: true),
                    Ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeliveryMethod = table.Column<int>(type: "integer", nullable: true),
                    PriorityLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    IsBusinessPosting = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Post_pkey", x => x.PostId);
                });

            migrationBuilder.CreateTable(
                name: "Subscription_Package",
                schema: "public",
                columns: table => new
                {
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Duration = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Subscription_Package_pkey", x => x.PackageId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Users_pkey", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Product_Type",
                schema: "public",
                columns: table => new
                {
                    ProductTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductTypeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Product_Type_pkey", x => x.ProductTypeId);
                    table.ForeignKey(
                        name: "fk_product_type_category",
                        column: x => x.CategoryId,
                        principalSchema: "public",
                        principalTable: "Category",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "Audit_Log",
                schema: "public",
                columns: table => new
                {
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserRole = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    TargetTable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Audit_Log_pkey", x => x.AuditId);
                    table.ForeignKey(
                        name: "fk_audit_log_userid",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Bank_Accounts",
                schema: "public",
                columns: table => new
                {
                    UserBankId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VerifyStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Bank_Accounts_pkey", x => x.UserBankId);
                    table.ForeignKey(
                        name: "fk_bank_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Business_Profile",
                schema: "public",
                columns: table => new
                {
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BusinessDescription = table.Column<string>(type: "text", nullable: true),
                    TaxCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BusinessAddress = table.Column<string>(type: "text", nullable: true),
                    Ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OperatingScope = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BusinessModel = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReputationScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Business_Profile_pkey", x => x.BusinessProfileId);
                    table.ForeignKey(
                        name: "fk_business_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                schema: "public",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    TargetType = table.Column<int>(type: "integer", nullable: true),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Notification_pkey", x => x.NotificationId);
                    table.ForeignKey(
                        name: "fk_notification_userid",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Offer",
                schema: "public",
                columns: table => new
                {
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiverId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    OfferQuantity = table.Column<int>(type: "integer", nullable: false),
                    OfferStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Offer_pkey", x => x.OfferId);
                    table.ForeignKey(
                        name: "fk_offer_post",
                        column: x => x.PostId,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "PostId");
                    table.ForeignKey(
                        name: "fk_offer_receiver",
                        column: x => x.ReceiverId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_offer_sender",
                        column: x => x.SenderId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "OTP",
                schema: "public",
                columns: table => new
                {
                    OtpId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Register"),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("OTP_pkey", x => x.OtpId);
                    table.ForeignKey(
                        name: "fk_otp_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Personal_Profile",
                schema: "public",
                columns: table => new
                {
                    PersonalProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RepresentativeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RepresentativeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RepresentativeDob = table.Column<DateOnly>(type: "date", nullable: true),
                    RepresentativeAddress = table.Column<string>(type: "text", nullable: true),
                    FrontIDCardImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BackIDCardImage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    ReputationScore = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Personal_Profile_pkey", x => x.PersonalProfileId);
                    table.ForeignKey(
                        name: "fk_personal_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_personal_verified_by",
                        column: x => x.VerifiedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Refresh_Token",
                schema: "public",
                columns: table => new
                {
                    RefreshTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "text", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Refresh_Token_pkey", x => x.RefreshTokenId);
                    table.ForeignKey(
                        name: "fk_refresh_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "User_Subscription",
                schema: "public",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("User_Subscription_pkey", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "fk_us_package",
                        column: x => x.PackageId,
                        principalSchema: "public",
                        principalTable: "Subscription_Package",
                        principalColumn: "PackageId");
                    table.ForeignKey(
                        name: "fk_us_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Wallet",
                schema: "public",
                columns: table => new
                {
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AvailableBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    HoldBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Wallet_pkey", x => x.WalletId);
                    table.ForeignKey(
                        name: "fk_wallet_user",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Product",
                schema: "public",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SpaceUsage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModelNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Length = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Width = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Height = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    FunctionalityStatus = table.Column<int>(type: "integer", nullable: true),
                    UsageDuration = table.Column<int>(type: "integer", nullable: true),
                    DamageLevel = table.Column<int>(type: "integer", nullable: true),
                    DetailDescription = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Product_pkey", x => x.ProductId);
                    table.ForeignKey(
                        name: "fk_product_brand",
                        column: x => x.BrandId,
                        principalSchema: "public",
                        principalTable: "Brand",
                        principalColumn: "BrandId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_product_category",
                        column: x => x.CategoryId,
                        principalSchema: "public",
                        principalTable: "Category",
                        principalColumn: "CategoryId");
                    table.ForeignKey(
                        name: "fk_product_post",
                        column: x => x.PostId,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "PostId");
                    table.ForeignKey(
                        name: "fk_product_type",
                        column: x => x.ProductTypeId,
                        principalSchema: "public",
                        principalTable: "Product_Type",
                        principalColumn: "ProductTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Product_Attribute",
                schema: "public",
                columns: table => new
                {
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DataType = table.Column<int>(type: "integer", nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    IsFilterable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Product_Attribute_pkey", x => x.AttributeId);
                    table.ForeignKey(
                        name: "fk_attr_product_type",
                        column: x => x.ProductTypeId,
                        principalSchema: "public",
                        principalTable: "Product_Type",
                        principalColumn: "ProductTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Business_Documents",
                schema: "public",
                columns: table => new
                {
                    BusinessDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocumentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: true),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: true),
                    VerifiedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    RejectReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Business_Documents_pkey", x => x.BusinessDocumentId);
                    table.ForeignKey(
                        name: "fk_business_doc_profile",
                        column: x => x.BusinessProfileId,
                        principalSchema: "public",
                        principalTable: "Business_Profile",
                        principalColumn: "BusinessProfileId");
                    table.ForeignKey(
                        name: "fk_business_doc_verified_by",
                        column: x => x.VerifiedBy,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Business_Product_Type",
                schema: "public",
                columns: table => new
                {
                    BusinessProductTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Business_Product_Type_pkey", x => x.BusinessProductTypeId);
                    table.ForeignKey(
                        name: "fk_bpt_business",
                        column: x => x.BusinessProfileId,
                        principalSchema: "public",
                        principalTable: "Business_Profile",
                        principalColumn: "BusinessProfileId");
                    table.ForeignKey(
                        name: "fk_bpt_product_type",
                        column: x => x.ProductTypeId,
                        principalSchema: "public",
                        principalTable: "Product_Type",
                        principalColumn: "ProductTypeId");
                });

            migrationBuilder.CreateTable(
                name: "Business_Service_Area",
                schema: "public",
                columns: table => new
                {
                    BusinessServiceAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ward = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Business_Service_Area_pkey", x => x.BusinessServiceAreaId);
                    table.ForeignKey(
                        name: "fk_bsa_business",
                        column: x => x.BusinessProfileId,
                        principalSchema: "public",
                        principalTable: "Business_Profile",
                        principalColumn: "BusinessProfileId");
                });

            migrationBuilder.CreateTable(
                name: "Negotiation",
                schema: "public",
                columns: table => new
                {
                    NegotiationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FinalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    NegotiationStatus = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Negotiation_pkey", x => x.NegotiationId);
                    table.ForeignKey(
                        name: "fk_neg_buyer",
                        column: x => x.BuyerId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_neg_offer",
                        column: x => x.OfferId,
                        principalSchema: "public",
                        principalTable: "Offer",
                        principalColumn: "OfferId");
                    table.ForeignKey(
                        name: "fk_neg_post",
                        column: x => x.PostId,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "PostId");
                    table.ForeignKey(
                        name: "fk_neg_seller",
                        column: x => x.SellerId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Withdrawal",
                schema: "public",
                columns: table => new
                {
                    WithdrawalId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserBankId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    WithdrawalStatus = table.Column<int>(type: "integer", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Withdrawal_pkey", x => x.WithdrawalId);
                    table.ForeignKey(
                        name: "fk_withdrawal_userbankid",
                        column: x => x.UserBankId,
                        principalSchema: "public",
                        principalTable: "Bank_Accounts",
                        principalColumn: "UserBankId");
                    table.ForeignKey(
                        name: "fk_withdrawal_walletid",
                        column: x => x.WalletId,
                        principalSchema: "public",
                        principalTable: "Wallet",
                        principalColumn: "WalletId");
                });

            migrationBuilder.CreateTable(
                name: "Product_Attribute_Option",
                schema: "public",
                columns: table => new
                {
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionValue = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Product_Attribute_Option_pkey", x => x.OptionId);
                    table.ForeignKey(
                        name: "fk_attr_option",
                        column: x => x.AttributeId,
                        principalSchema: "public",
                        principalTable: "Product_Attribute",
                        principalColumn: "AttributeId");
                });

            migrationBuilder.CreateTable(
                name: "Agreement_Form",
                schema: "public",
                columns: table => new
                {
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    NegotiationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PSnapshot = table.Column<string>(type: "json", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    InitialPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FinalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AgreementType = table.Column<int>(type: "integer", nullable: true),
                    AgreementDetailsJsonb = table.Column<string>(type: "json", nullable: true),
                    PaymentType = table.Column<int>(type: "integer", nullable: true),
                    AgreementStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    BuyerConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SellerConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Agreement_Form_pkey", x => x.AgreementId);
                    table.ForeignKey(
                        name: "fk_agreement_buyer",
                        column: x => x.BuyerId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_agreement_neg",
                        column: x => x.NegotiationId,
                        principalSchema: "public",
                        principalTable: "Negotiation",
                        principalColumn: "NegotiationId");
                    table.ForeignKey(
                        name: "fk_agreement_post",
                        column: x => x.PostId,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "PostId");
                    table.ForeignKey(
                        name: "fk_agreement_seller",
                        column: x => x.SellerId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "public",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    NegotiationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageContent = table.Column<string>(type: "text", nullable: true),
                    MessageType = table.Column<int>(type: "integer", nullable: true),
                    OfferPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    OfferQuantity = table.Column<int>(type: "integer", nullable: false),
                    OfferStatus = table.Column<int>(type: "integer", nullable: true),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Messages_pkey", x => x.MessageId);
                    table.ForeignKey(
                        name: "fk_msg_neg",
                        column: x => x.NegotiationId,
                        principalSchema: "public",
                        principalTable: "Negotiation",
                        principalColumn: "NegotiationId");
                    table.ForeignKey(
                        name: "fk_msg_sender",
                        column: x => x.SenderId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Product_Attribute_Value",
                schema: "public",
                columns: table => new
                {
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    InputType = table.Column<int>(type: "integer", nullable: true),
                    ValueBoolean = table.Column<bool>(type: "boolean", nullable: true),
                    ValueText = table.Column<string>(type: "text", nullable: true),
                    ValueNumber = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Product_Attribute_Value_pkey", x => new { x.ProductId, x.AttributeId });
                    table.ForeignKey(
                        name: "fk_pav_attribute",
                        column: x => x.AttributeId,
                        principalSchema: "public",
                        principalTable: "Product_Attribute",
                        principalColumn: "AttributeId");
                    table.ForeignKey(
                        name: "fk_pav_option",
                        column: x => x.OptionId,
                        principalSchema: "public",
                        principalTable: "Product_Attribute_Option",
                        principalColumn: "OptionId");
                    table.ForeignKey(
                        name: "fk_pav_product",
                        column: x => x.ProductId,
                        principalSchema: "public",
                        principalTable: "Product",
                        principalColumn: "ProductId");
                });

            migrationBuilder.CreateTable(
                name: "Appointment",
                schema: "public",
                columns: table => new
                {
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentType = table.Column<int>(type: "integer", nullable: true),
                    AppointmentStatus = table.Column<int>(type: "integer", nullable: true),
                    BuyerCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SellerCheckAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Appointment_pkey", x => x.AppointmentId);
                    table.ForeignKey(
                        name: "fk_appt_agreement",
                        column: x => x.AgreementId,
                        principalSchema: "public",
                        principalTable: "Agreement_Form",
                        principalColumn: "AgreementId");
                });

            migrationBuilder.CreateTable(
                name: "Order",
                schema: "public",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    OriginalTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FinalTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    AmountRemaining = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: true),
                    OrderStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Order_pkey", x => x.OrderId);
                    table.ForeignKey(
                        name: "fk_order_agreementid",
                        column: x => x.AgreementId,
                        principalSchema: "public",
                        principalTable: "Agreement_Form",
                        principalColumn: "AgreementId");
                    table.ForeignKey(
                        name: "fk_order_postid",
                        column: x => x.PostId,
                        principalSchema: "public",
                        principalTable: "Post",
                        principalColumn: "PostId");
                });

            migrationBuilder.CreateTable(
                name: "Collection_Appointment",
                schema: "public",
                columns: table => new
                {
                    CollectionAppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PickupAddress = table.Column<string>(type: "text", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: true),
                    DeliveryMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Collection_Appointment_pkey", x => x.CollectionAppointmentId);
                    table.ForeignKey(
                        name: "fk_col_appt",
                        column: x => x.AppointmentId,
                        principalSchema: "public",
                        principalTable: "Appointment",
                        principalColumn: "AppointmentId");
                });

            migrationBuilder.CreateTable(
                name: "Inspection_Appointment",
                schema: "public",
                columns: table => new
                {
                    InspectionAppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionAddress = table.Column<string>(type: "text", nullable: true),
                    InspectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Inspection_Appointment_pkey", x => x.InspectionAppointmentId);
                    table.ForeignKey(
                        name: "fk_insp_appt",
                        column: x => x.AppointmentId,
                        principalSchema: "public",
                        principalTable: "Appointment",
                        principalColumn: "AppointmentId");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                schema: "public",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    PayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentType = table.Column<int>(type: "integer", nullable: true),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<int>(type: "integer", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Payment_pkey", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_Payment_AgreementId",
                        column: x => x.AgreementId,
                        principalSchema: "public",
                        principalTable: "Agreement_Form",
                        principalColumn: "AgreementId");
                    table.ForeignKey(
                        name: "FK_Payment_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK_Payment_PayerId",
                        column: x => x.PayerId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Review",
                schema: "public",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevieweeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    ReviewStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Review_pkey", x => x.ReviewId);
                    table.ForeignKey(
                        name: "fk_review_orderid",
                        column: x => x.OrderId,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "fk_review_revieweeid",
                        column: x => x.RevieweeId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_review_reviewerid",
                        column: x => x.ReviewerId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Shipment",
                schema: "public",
                columns: table => new
                {
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CollectionAppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryMethod = table.Column<int>(type: "integer", nullable: true),
                    ShipmentStatus = table.Column<int>(type: "integer", nullable: true),
                    FromName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FromPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PickupAddress = table.Column<string>(type: "text", nullable: true),
                    PickedUpAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ToName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ToPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "text", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Shipment_pkey", x => x.ShipmentId);
                    table.ForeignKey(
                        name: "fk_shipment_collection_appointment",
                        column: x => x.CollectionAppointmentId,
                        principalSchema: "public",
                        principalTable: "Collection_Appointment",
                        principalColumn: "CollectionAppointmentId");
                    table.ForeignKey(
                        name: "fk_shipment_orderid",
                        column: x => x.OrderId,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "OrderId");
                });

            migrationBuilder.CreateTable(
                name: "Inspection_Form",
                schema: "public",
                columns: table => new
                {
                    InspectionFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionAppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InspectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OperatingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AppearanceStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PartsStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MatchStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InspectorNotes = table.Column<string>(type: "text", nullable: true),
                    Conclusion = table.Column<string>(type: "text", nullable: true),
                    OriginalPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    SuggestedPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CollectAction = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InspectionStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Inspection_Form_pkey", x => x.InspectionFormId);
                    table.ForeignKey(
                        name: "fk_insp_form",
                        column: x => x.InspectionAppointmentId,
                        principalSchema: "public",
                        principalTable: "Inspection_Appointment",
                        principalColumn: "InspectionAppointmentId");
                    table.ForeignKey(
                        name: "fk_insp_form_inspectorid",
                        column: x => x.InspectorId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Payment_Transaction",
                schema: "public",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PayOSOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PayOSPaymentLinkId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PayOSTransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CheckoutUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PaymentTransactionStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Payment_Transaction_pkey", x => x.PaymentTransactionId);
                    table.ForeignKey(
                        name: "fk_payment_transaction_paymentid",
                        column: x => x.PaymentId,
                        principalSchema: "public",
                        principalTable: "Payment",
                        principalColumn: "PaymentId");
                    table.ForeignKey(
                        name: "fk_payment_transaction_userid",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Wallet_Transaction",
                schema: "public",
                columns: table => new
                {
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromWalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    ToWalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<int>(type: "integer", nullable: true),
                    TransactionType = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    WalletTransactionStatus = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Wallet_Transaction_pkey", x => x.WalletTransactionId);
                    table.ForeignKey(
                        name: "fk_wallet_transaction_fromwalletid",
                        column: x => x.FromWalletId,
                        principalSchema: "public",
                        principalTable: "Wallet",
                        principalColumn: "WalletId");
                    table.ForeignKey(
                        name: "fk_wallet_transaction_paymentid",
                        column: x => x.PaymentId,
                        principalSchema: "public",
                        principalTable: "Payment",
                        principalColumn: "PaymentId");
                    table.ForeignKey(
                        name: "fk_wallet_transaction_towalletid",
                        column: x => x.ToWalletId,
                        principalSchema: "public",
                        principalTable: "Wallet",
                        principalColumn: "WalletId");
                });

            migrationBuilder.CreateTable(
                name: "Dispute",
                schema: "public",
                columns: table => new
                {
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModeratorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisputeTargetType = table.Column<int>(type: "integer", nullable: true),
                    DisputeCategory = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DisputeStatus = table.Column<int>(type: "integer", nullable: true),
                    ModeratorNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Dispute_pkey", x => x.DisputeId);
                    table.ForeignKey(
                        name: "fk_dispute_moderatorid",
                        column: x => x.ModeratorId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_dispute_orderid",
                        column: x => x.OrderId,
                        principalSchema: "public",
                        principalTable: "Order",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "fk_dispute_reviewid",
                        column: x => x.ReviewId,
                        principalSchema: "public",
                        principalTable: "Review",
                        principalColumn: "ReviewId");
                    table.ForeignKey(
                        name: "fk_dispute_senderid",
                        column: x => x.SenderId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "fk_dispute_targetuserid",
                        column: x => x.TargetUserId,
                        principalSchema: "public",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "GHN_Shipment",
                schema: "public",
                columns: table => new
                {
                    GHNShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GHNOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ClientOrderCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GHNStatusCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ServiceId = table.Column<int>(type: "integer", nullable: true),
                    ServiceTypeId = table.Column<int>(type: "integer", nullable: true),
                    FromDistrictId = table.Column<int>(type: "integer", nullable: true),
                    FromWardCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToDistrictId = table.Column<int>(type: "integer", nullable: true),
                    ToWardCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Weight = table.Column<int>(type: "integer", nullable: true),
                    Length = table.Column<int>(type: "integer", nullable: true),
                    Width = table.Column<int>(type: "integer", nullable: true),
                    Height = table.Column<int>(type: "integer", nullable: true),
                    CODAmount = table.Column<int>(type: "integer", nullable: true),
                    PaymentTypeId = table.Column<int>(type: "integer", nullable: false),
                    InsuranceValue = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    RequiredNote = table.Column<int>(type: "integer", nullable: true),
                    GHNServiceFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    GHNCodFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    GHNTotalFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("GHN_Shipment_pkey", x => x.GHNShipmentId);
                    table.ForeignKey(
                        name: "fk_ghn_shipment_shipmentid",
                        column: x => x.ShipmentId,
                        principalSchema: "public",
                        principalTable: "Shipment",
                        principalColumn: "ShipmentId");
                });

            migrationBuilder.CreateTable(
                name: "Wallet_Ledger",
                schema: "public",
                columns: table => new
                {
                    LedgerId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("Wallet_Ledger_pkey", x => x.LedgerId);
                    table.ForeignKey(
                        name: "fk_wallet_ledger_walletid",
                        column: x => x.WalletId,
                        principalSchema: "public",
                        principalTable: "Wallet",
                        principalColumn: "WalletId");
                    table.ForeignKey(
                        name: "fk_wallet_ledger_wallettransactionid",
                        column: x => x.WalletTransactionId,
                        principalSchema: "public",
                        principalTable: "Wallet_Transaction",
                        principalColumn: "WalletTransactionId");
                });

            migrationBuilder.CreateIndex(
                name: "Agreement_Form_NegotiationId_key",
                schema: "public",
                table: "Agreement_Form",
                column: "NegotiationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_agreement_buyer",
                schema: "public",
                table: "Agreement_Form",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "idx_agreement_post",
                schema: "public",
                table: "Agreement_Form",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "idx_agreement_seller",
                schema: "public",
                table: "Agreement_Form",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "idx_appointment_agreement",
                schema: "public",
                table: "Appointment",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "idx_appointment_status",
                schema: "public",
                table: "Appointment",
                column: "AppointmentStatus");

            migrationBuilder.CreateIndex(
                name: "idx_audit_created",
                schema: "public",
                table: "Audit_Log",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_audit_target",
                schema: "public",
                table: "Audit_Log",
                columns: new[] { "TargetTable", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_user",
                schema: "public",
                table: "Audit_Log",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bank_Accounts_UserId",
                schema: "public",
                table: "Bank_Accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "uq_bank_account",
                schema: "public",
                table: "Bank_Accounts",
                column: "AccountNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brand_BrandName",
                schema: "public",
                table: "Brand",
                column: "BrandName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_business_document_profile",
                schema: "public",
                table: "Business_Documents",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Business_Documents_VerifiedBy",
                schema: "public",
                table: "Business_Documents",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "idx_business_product_type_business",
                schema: "public",
                table: "Business_Product_Type",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "idx_business_product_type_product",
                schema: "public",
                table: "Business_Product_Type",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "uq_bpt",
                schema: "public",
                table: "Business_Product_Type",
                columns: new[] { "BusinessProfileId", "ProductTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_business_product_type",
                schema: "public",
                table: "Business_Product_Type",
                columns: new[] { "BusinessProfileId", "ProductTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Business_Profile_UserId_key",
                schema: "public",
                table: "Business_Profile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_business_profile_userid",
                schema: "public",
                table: "Business_Profile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_business_service_area_business",
                schema: "public",
                table: "Business_Service_Area",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "uq_bsa",
                schema: "public",
                table: "Business_Service_Area",
                columns: new[] { "BusinessProfileId", "City", "District", "Ward" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_business_service_area",
                schema: "public",
                table: "Business_Service_Area",
                columns: new[] { "BusinessProfileId", "City", "District", "Ward" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_category_name",
                schema: "public",
                table: "Category",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_category_name",
                schema: "public",
                table: "Category",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "Collection_Appointment_AppointmentId_key",
                schema: "public",
                table: "Collection_Appointment",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_collection_appointment",
                schema: "public",
                table: "Collection_Appointment",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "idx_dispute_moderator",
                schema: "public",
                table: "Dispute",
                column: "ModeratorId");

            migrationBuilder.CreateIndex(
                name: "idx_dispute_order",
                schema: "public",
                table: "Dispute",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "idx_dispute_order_status",
                schema: "public",
                table: "Dispute",
                columns: new[] { "OrderId", "DisputeStatus" });

            migrationBuilder.CreateIndex(
                name: "idx_dispute_sender",
                schema: "public",
                table: "Dispute",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "idx_dispute_status",
                schema: "public",
                table: "Dispute",
                column: "DisputeStatus");

            migrationBuilder.CreateIndex(
                name: "idx_dispute_target_user",
                schema: "public",
                table: "Dispute",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dispute_ReviewId",
                schema: "public",
                table: "Dispute",
                column: "ReviewId");

            migrationBuilder.CreateIndex(
                name: "GHN_Shipment_ShipmentId_key",
                schema: "public",
                table: "GHN_Shipment",
                column: "ShipmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_ghn_client_order_code",
                schema: "public",
                table: "GHN_Shipment",
                column: "ClientOrderCode");

            migrationBuilder.CreateIndex(
                name: "uq_ghn_order_code",
                schema: "public",
                table: "GHN_Shipment",
                column: "GHNOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ghn_order_code",
                schema: "public",
                table: "GHN_Shipment",
                column: "GHNOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inspection_appointment",
                schema: "public",
                table: "Inspection_Appointment",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "Inspection_Appointment_AppointmentId_key",
                schema: "public",
                table: "Inspection_Appointment",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_inspection_inspector",
                schema: "public",
                table: "Inspection_Form",
                column: "InspectorId");

            migrationBuilder.CreateIndex(
                name: "idx_inspection_order",
                schema: "public",
                table: "Inspection_Form",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "Inspection_Form_InspectionAppointmentId_key",
                schema: "public",
                table: "Inspection_Form",
                column: "InspectionAppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_media_target",
                schema: "public",
                table: "Media",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "idx_message_created",
                schema: "public",
                table: "Messages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_message_negotiation",
                schema: "public",
                table: "Messages",
                column: "NegotiationId");

            migrationBuilder.CreateIndex(
                name: "idx_message_sender",
                schema: "public",
                table: "Messages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "idx_negotiation_buyer",
                schema: "public",
                table: "Negotiation",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "idx_negotiation_offer",
                schema: "public",
                table: "Negotiation",
                column: "OfferId");

            migrationBuilder.CreateIndex(
                name: "idx_negotiation_post",
                schema: "public",
                table: "Negotiation",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "idx_negotiation_seller",
                schema: "public",
                table: "Negotiation",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "Negotiation_OfferId_key",
                schema: "public",
                table: "Negotiation",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_notification_read",
                schema: "public",
                table: "Notification",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "idx_notification_target",
                schema: "public",
                table: "Notification",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "idx_notification_user",
                schema: "public",
                table: "Notification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_offer_post",
                schema: "public",
                table: "Offer",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "idx_offer_receiver_status",
                schema: "public",
                table: "Offer",
                columns: new[] { "ReceiverId", "OfferStatus" });

            migrationBuilder.CreateIndex(
                name: "idx_offer_sender_status",
                schema: "public",
                table: "Offer",
                columns: new[] { "SenderId", "OfferStatus" });

            migrationBuilder.CreateIndex(
                name: "idx_offer_status",
                schema: "public",
                table: "Offer",
                column: "OfferStatus");

            migrationBuilder.CreateIndex(
                name: "idx_order_agreement",
                schema: "public",
                table: "Order",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "idx_order_payment_status",
                schema: "public",
                table: "Order",
                column: "PaymentStatus");

            migrationBuilder.CreateIndex(
                name: "idx_order_post",
                schema: "public",
                table: "Order",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "idx_order_status",
                schema: "public",
                table: "Order",
                column: "OrderStatus");

            migrationBuilder.CreateIndex(
                name: "Order_AgreementId_key",
                schema: "public",
                table: "Order",
                column: "AgreementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_otp_userid",
                schema: "public",
                table: "OTP",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_agreement",
                schema: "public",
                table: "Payment",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_order",
                schema: "public",
                table: "Payment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_payer",
                schema: "public",
                table: "Payment",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_transaction_payment",
                schema: "public",
                table: "Payment_Transaction",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "idx_payment_transaction_user",
                schema: "public",
                table: "Payment_Transaction",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "uq_payos_order",
                schema: "public",
                table: "Payment_Transaction",
                column: "PayOSOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payos_transaction",
                schema: "public",
                table: "Payment_Transaction",
                column: "PayOSTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_transaction_id",
                schema: "public",
                table: "Payment_Transaction",
                column: "PayOSTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_payment_transaction_order",
                schema: "public",
                table: "Payment_Transaction",
                column: "PayOSOrderCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Personal_Profile_VerifiedBy",
                schema: "public",
                table: "Personal_Profile",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "Personal_Profile_UserId_key",
                schema: "public",
                table: "Personal_Profile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_personal_profile_userid",
                schema: "public",
                table: "Personal_Profile",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_policy_type_version",
                schema: "public",
                table: "Platform_Policy",
                columns: new[] { "PolicyType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_policy_type_version",
                schema: "public",
                table: "Platform_Policy",
                columns: new[] { "PolicyType", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_post_city",
                schema: "public",
                table: "Post",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "idx_post_owner",
                schema: "public",
                table: "Post",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "idx_post_status",
                schema: "public",
                table: "Post",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "idx_post_type",
                schema: "public",
                table: "Post",
                column: "PostType");

            migrationBuilder.CreateIndex(
                name: "IX_Product_BrandId",
                schema: "public",
                table: "Product",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_CategoryId",
                schema: "public",
                table: "Product",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProductTypeId",
                schema: "public",
                table: "Product",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "Product_PostId_key",
                schema: "public",
                table: "Product",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_product_attribute_type",
                schema: "public",
                table: "Product_Attribute",
                column: "ProductTypeId");

            migrationBuilder.CreateIndex(
                name: "idx_product_attribute_option_attribute",
                schema: "public",
                table: "Product_Attribute_Option",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Attribute_Value_AttributeId",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_Product_Attribute_Value_OptionId",
                schema: "public",
                table: "Product_Attribute_Value",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "ux_product_type_name",
                schema: "public",
                table: "Product_Type",
                columns: new[] { "CategoryId", "ProductTypeName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_refresh_token_userid",
                schema: "public",
                table: "Refresh_Token",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "idx_review_reviewee",
                schema: "public",
                table: "Review",
                column: "RevieweeId");

            migrationBuilder.CreateIndex(
                name: "idx_review_reviewer",
                schema: "public",
                table: "Review",
                column: "ReviewerId");

            migrationBuilder.CreateIndex(
                name: "Review_OrderId_key",
                schema: "public",
                table: "Review",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_shipment_order",
                schema: "public",
                table: "Shipment",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_CollectionAppointmentId",
                schema: "public",
                table: "Shipment",
                column: "CollectionAppointmentId");

            migrationBuilder.CreateIndex(
                name: "ux_subscription_package_name",
                schema: "public",
                table: "Subscription_Package",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_user_subscription_package",
                schema: "public",
                table: "User_Subscription",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "idx_user_subscription_user",
                schema: "public",
                table: "User_Subscription",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "uq_wallet",
                schema: "public",
                table: "Wallet",
                columns: new[] { "UserId", "WalletType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_wallet_user_type",
                schema: "public",
                table: "Wallet",
                columns: new[] { "UserId", "WalletType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_wallet_ledger_transaction",
                schema: "public",
                table: "Wallet_Ledger",
                column: "WalletTransactionId");

            migrationBuilder.CreateIndex(
                name: "idx_wallet_ledger_wallet",
                schema: "public",
                table: "Wallet_Ledger",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "idx_wallet_transaction_from",
                schema: "public",
                table: "Wallet_Transaction",
                column: "FromWalletId");

            migrationBuilder.CreateIndex(
                name: "idx_wallet_transaction_payment",
                schema: "public",
                table: "Wallet_Transaction",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "idx_wallet_transaction_reference",
                schema: "public",
                table: "Wallet_Transaction",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "idx_wallet_transaction_to",
                schema: "public",
                table: "Wallet_Transaction",
                column: "ToWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawal_UserBankId",
                schema: "public",
                table: "Withdrawal",
                column: "UserBankId");

            migrationBuilder.CreateIndex(
                name: "IX_Withdrawal_WalletId",
                schema: "public",
                table: "Withdrawal",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audit_Log",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Business_Documents",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Business_Product_Type",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Business_Service_Area",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Dispute",
                schema: "public");

            migrationBuilder.DropTable(
                name: "GHN_Shipment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Inspection_Form",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Media",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Notification",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OTP",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payment_Transaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Personal_Profile",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Platform_Policy",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Product_Attribute_Value",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Refresh_Token",
                schema: "public");

            migrationBuilder.DropTable(
                name: "User_Subscription",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Wallet_Ledger",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Withdrawal",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Business_Profile",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Review",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Shipment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Inspection_Appointment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Product_Attribute_Option",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Product",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Subscription_Package",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Wallet_Transaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Bank_Accounts",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Collection_Appointment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Product_Attribute",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Brand",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Wallet",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Payment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Appointment",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Product_Type",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Order",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Category",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Agreement_Form",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Negotiation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Offer",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Post",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public");
        }
    }
}
