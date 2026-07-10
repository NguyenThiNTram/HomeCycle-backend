using System;
using System.Collections.Generic;
using HomeCycle.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure.DbContexts;

public partial class HomeCycleDbContext : DbContext
{
    public HomeCycleDbContext(DbContextOptions<HomeCycleDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agreement_Form> Agreement_Forms { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<Audit_Log> Audit_Logs { get; set; }

    public virtual DbSet<Bank_Account> Bank_Accounts { get; set; }

    public virtual DbSet<Business_Document> Business_Documents { get; set; }

    public virtual DbSet<Business_Product_Type> Business_Product_Types { get; set; }

    public virtual DbSet<Business_Profile> Business_Profiles { get; set; }

    public virtual DbSet<Business_Service_Area> Business_Service_Areas { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Collection_Appointment> Collection_Appointments { get; set; }

    public virtual DbSet<Dispute> Disputes { get; set; }

    public virtual DbSet<GHN_Shipment> GHN_Shipments { get; set; }

    public virtual DbSet<Inspection_Appointment> Inspection_Appointments { get; set; }

    public virtual DbSet<Inspection_Form> Inspection_Forms { get; set; }

    public virtual DbSet<Media> Media { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<Negotiation> Negotiations { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<OTP> OTPs { get; set; }

    public virtual DbSet<Offer> Offers { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Payment_Transaction> Payment_Transactions { get; set; }

    public virtual DbSet<Personal_Profile> Personal_Profiles { get; set; }

    public virtual DbSet<Platform_Policy> Platform_Policies { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Product_Attribute> Product_Attributes { get; set; }

    public virtual DbSet<Product_Attribute_Option> Product_Attribute_Options { get; set; }

    public virtual DbSet<Product_Attribute_Value> Product_Attribute_Values { get; set; }

    public virtual DbSet<Product_Type> Product_Types { get; set; }

    public virtual DbSet<Refresh_Token> Refresh_Tokens { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Shipment> Shipments { get; set; }

    public virtual DbSet<Subscription_Package> Subscription_Packages { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<User_Subscription> User_Subscriptions { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<Wallet_Ledger> Wallet_Ledgers { get; set; }

    public virtual DbSet<Wallet_Transaction> Wallet_Transactions { get; set; }

    public virtual DbSet<Withdrawal> Withdrawals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresEnum("auth", "aal_level", new[] { "aal1", "aal2", "aal3" })
            .HasPostgresEnum("auth", "code_challenge_method", new[] { "s256", "plain" })
            .HasPostgresEnum("auth", "factor_status", new[] { "unverified", "verified" })
            .HasPostgresEnum("auth", "factor_type", new[] { "totp", "webauthn", "phone" })
            .HasPostgresEnum("auth", "oauth_authorization_status", new[] { "pending", "approved", "denied", "expired" })
            .HasPostgresEnum("auth", "oauth_client_type", new[] { "public", "confidential" })
            .HasPostgresEnum("auth", "oauth_registration_type", new[] { "dynamic", "manual" })
            .HasPostgresEnum("auth", "oauth_response_type", new[] { "code" })
            .HasPostgresEnum("auth", "one_time_token_type", new[] { "confirmation_token", "reauthentication_token", "recovery_token", "email_change_token_new", "email_change_token_current", "phone_change_token" })
            .HasPostgresEnum("realtime", "action", new[] { "INSERT", "UPDATE", "DELETE", "TRUNCATE", "ERROR" })
            .HasPostgresEnum("realtime", "equality_op", new[] { "eq", "neq", "lt", "lte", "gt", "gte", "in", "like", "ilike", "is", "match", "imatch", "isdistinct" })
            .HasPostgresEnum("storage", "buckettype", new[] { "STANDARD", "ANALYTICS", "VECTOR" })
            .HasPostgresExtension("extensions", "pg_stat_statements")
            .HasPostgresExtension("extensions", "pgcrypto")
            .HasPostgresExtension("extensions", "uuid-ossp")
            .HasPostgresExtension("vault", "supabase_vault");

        modelBuilder.Entity<Agreement_Form>(entity =>
        {
            entity.HasKey(e => e.AgreementId).HasName("Agreement_Form_pkey");

            entity.Property(e => e.AgreementId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Buyer).WithMany(p => p.Agreement_FormBuyers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_agreement_buyer");

            entity.HasOne(d => d.Negotiation).WithOne(p => p.Agreement_Form)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_agreement_neg");

            entity.HasOne(d => d.Post).WithMany(p => p.Agreement_Forms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_agreement_post");

            entity.HasOne(d => d.Seller).WithMany(p => p.Agreement_FormSellers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_agreement_seller");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(e => e.AppointmentId).HasName("Appointment_pkey");

            entity.Property(e => e.AppointmentId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Agreement).WithMany(p => p.Appointments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_appt_agreement");
        });

        modelBuilder.Entity<Audit_Log>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("Audit_Log_pkey");

            entity.Property(e => e.AuditId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Audit_Logs).HasConstraintName("fk_audit_log_userid");
        });

        modelBuilder.Entity<Bank_Account>(entity =>
        {
            entity.HasKey(e => e.UserBankId).HasName("Bank_Accounts_pkey");

            entity.Property(e => e.UserBankId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Bank_Accounts).HasConstraintName("fk_bank_user");
        });

        modelBuilder.Entity<Business_Document>(entity =>
        {
            entity.HasKey(e => e.BusinessDocumentId).HasName("Business_Documents_pkey");

            entity.Property(e => e.BusinessDocumentId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.VerifiedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.BusinessProfile).WithMany(p => p.Business_Documents)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_business_doc_profile");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.Business_Documents).HasConstraintName("fk_business_doc_verified_by");
        });

        modelBuilder.Entity<Business_Product_Type>(entity =>
        {
            entity.HasKey(e => e.BusinessProductTypeId).HasName("Business_Product_Type_pkey");

            entity.Property(e => e.BusinessProductTypeId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.BusinessProfile).WithMany(p => p.Business_Product_Types)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bpt_business");

            entity.HasOne(d => d.ProductType).WithMany(p => p.Business_Product_Types)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bpt_product_type");
        });

        modelBuilder.Entity<Business_Profile>(entity =>
        {
            entity.HasKey(e => e.BusinessProfileId).HasName("Business_Profile_pkey");

            entity.Property(e => e.BusinessProfileId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ReputationScore).HasDefaultValue(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithOne(p => p.Business_Profile)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_business_user");
        });

        modelBuilder.Entity<Business_Service_Area>(entity =>
        {
            entity.HasKey(e => e.BusinessServiceAreaId).HasName("Business_Service_Area_pkey");

            entity.Property(e => e.BusinessServiceAreaId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.BusinessProfile).WithMany(p => p.Business_Service_Areas)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_bsa_business");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("Category_pkey");

            entity.Property(e => e.CategoryId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Collection_Appointment>(entity =>
        {
            entity.HasKey(e => e.CollectionAppointmentId).HasName("Collection_Appointment_pkey");

            entity.Property(e => e.CollectionAppointmentId).ValueGeneratedNever();

            entity.HasOne(d => d.Appointment).WithOne(p => p.Collection_Appointment)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_col_appt");
        });

        modelBuilder.Entity<Dispute>(entity =>
        {
            entity.HasKey(e => e.DisputeId).HasName("Dispute_pkey");

            entity.Property(e => e.DisputeId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Moderator).WithMany(p => p.DisputeModerators).HasConstraintName("fk_dispute_moderatorid");

            entity.HasOne(d => d.Order).WithMany(p => p.Disputes).HasConstraintName("fk_dispute_orderid");

            entity.HasOne(d => d.Review).WithMany(p => p.Disputes).HasConstraintName("fk_dispute_reviewid");

            entity.HasOne(d => d.Sender).WithMany(p => p.DisputeSenders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_dispute_senderid");

            entity.HasOne(d => d.TargetUser).WithMany(p => p.DisputeTargetUsers).HasConstraintName("fk_dispute_targetuserid");
        });

        modelBuilder.Entity<GHN_Shipment>(entity =>
        {
            entity.HasKey(e => e.GHNShipmentId).HasName("GHN_Shipment_pkey");

            entity.Property(e => e.GHNShipmentId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.InsuranceValue).HasDefaultValue(0);

            entity.HasOne(d => d.Shipment).WithOne(p => p.GHN_Shipment)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ghn_shipment_shipmentid");
        });

        modelBuilder.Entity<Inspection_Appointment>(entity =>
        {
            entity.HasKey(e => e.InspectionAppointmentId).HasName("Inspection_Appointment_pkey");

            entity.Property(e => e.InspectionAppointmentId).ValueGeneratedNever();

            entity.HasOne(d => d.Appointment).WithOne(p => p.Inspection_Appointment)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_insp_appt");
        });

        modelBuilder.Entity<Inspection_Form>(entity =>
        {
            entity.HasKey(e => e.InspectionFormId).HasName("Inspection_Form_pkey");

            entity.Property(e => e.InspectionFormId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.InspectionAppointment).WithOne(p => p.Inspection_Form)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_insp_form");

            entity.HasOne(d => d.Inspector).WithMany(p => p.Inspection_Forms)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_insp_form_inspectorid");
        });

        modelBuilder.Entity<Media>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("Media_pkey");

            entity.Property(e => e.MediaId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("Messages_pkey");

            entity.Property(e => e.MessageId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.Negotiation).WithMany(p => p.Messages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_msg_neg");

            entity.HasOne(d => d.Sender).WithMany(p => p.Messages)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_msg_sender");
        });

        modelBuilder.Entity<Negotiation>(entity =>
        {
            entity.HasKey(e => e.NegotiationId).HasName("Negotiation_pkey");

            entity.Property(e => e.NegotiationId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Buyer).WithMany(p => p.NegotiationBuyers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_neg_buyer");

            entity.HasOne(d => d.Offer).WithOne(p => p.Negotiation)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_neg_offer");

            entity.HasOne(d => d.Post).WithMany(p => p.Negotiations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_neg_post");

            entity.HasOne(d => d.Seller).WithMany(p => p.NegotiationSellers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_neg_seller");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("Notification_pkey");

            entity.Property(e => e.NotificationId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsRead).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_notification_userid");
        });

        modelBuilder.Entity<OTP>(entity =>
        {
            entity.HasKey(e => e.OtpId).HasName("OTP_pkey");

            entity.Property(e => e.OtpId).ValueGeneratedNever();

            entity.Property(e => e.Email);
            entity.Property(e => e.Purpose).IsRequired().HasDefaultValue("Register");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ExpiredAt);
            entity.Property(e => e.IsUsed).HasDefaultValue(false);

            //entity.HasOne(d => d.User).WithMany(p => p.OTPs)
            //    .OnDelete(DeleteBehavior.ClientSetNull)
            //    .HasConstraintName("fk_otp_user");
            entity.HasOne(d => d.User)
                .WithMany(p => p.OTPs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_otp_user");
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.HasKey(e => e.OfferId).HasName("Offer_pkey");

            entity.Property(e => e.OfferId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Post).WithMany(p => p.Offers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_offer_post");

            entity.HasOne(d => d.Receiver).WithMany(p => p.OfferReceivers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_offer_receiver");

            entity.HasOne(d => d.Sender).WithMany(p => p.OfferSenders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_offer_sender");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("Order_pkey");

            entity.Property(e => e.OrderId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Agreement).WithOne(p => p.Order)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_agreementid");

            entity.HasOne(d => d.Post).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_postid");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("Payment_pkey");

            entity.Property(e => e.PaymentId).ValueGeneratedNever();

            entity.HasOne(d => d.Agreement).WithMany(p => p.Payments).HasConstraintName("FK_Payment_AgreementId");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments).HasConstraintName("FK_Payment_OrderId");

            entity.HasOne(d => d.Payer).WithMany(p => p.Payments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payment_PayerId");
        });

        modelBuilder.Entity<Payment_Transaction>(entity =>
        {
            entity.HasKey(e => e.PaymentTransactionId).HasName("Payment_Transaction_pkey");

            entity.Property(e => e.PaymentTransactionId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Payment).WithMany(p => p.Payment_Transactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_payment_transaction_paymentid");

            entity.HasOne(d => d.User).WithMany(p => p.Payment_Transactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_payment_transaction_userid");
        });

        modelBuilder.Entity<Personal_Profile>(entity =>
        {
            entity.HasKey(e => e.PersonalProfileId).HasName("Personal_Profile_pkey");

            entity.Property(e => e.PersonalProfileId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.ReputationScore).HasDefaultValue(100);
            entity.Property(e => e.VerifiedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithOne(p => p.Personal_ProfileUser)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_personal_user");

            entity.HasOne(d => d.VerifiedByNavigation).WithMany(p => p.Personal_ProfileVerifiedByNavigations).HasConstraintName("fk_personal_verified_by");
        });

        modelBuilder.Entity<Platform_Policy>(entity =>
        {
            entity.HasKey(e => e.PolicyId).HasName("Platform_Policy_pkey");

            entity.Property(e => e.PolicyId).ValueGeneratedNever();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("Post_pkey");

            entity.Property(e => e.PostId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsBusinessPosting).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("Product_pkey");

            entity.Property(e => e.ProductId).ValueGeneratedNever();

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_category");

            entity.HasOne(d => d.Post).WithOne(p => p.Product)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_post");

            entity.HasOne(d => d.ProductType).WithMany(p => p.Products)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_type");
        });

        modelBuilder.Entity<Product_Attribute>(entity =>
        {
            entity.HasKey(e => e.AttributeId).HasName("Product_Attribute_pkey");

            entity.Property(e => e.AttributeId).ValueGeneratedNever();
            entity.Property(e => e.IsFilterable).HasDefaultValue(false);
            entity.Property(e => e.IsRequired).HasDefaultValue(false);

            entity.HasOne(d => d.ProductType).WithMany(p => p.Product_Attributes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_attr_product_type");
        });

        modelBuilder.Entity<Product_Attribute_Option>(entity =>
        {
            entity.HasKey(e => e.OptionId).HasName("Product_Attribute_Option_pkey");

            entity.Property(e => e.OptionId).ValueGeneratedNever();
            entity.Property(e => e.IsDefault).HasDefaultValue(false);

            entity.HasOne(d => d.Attribute).WithMany(p => p.Product_Attribute_Options)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_attr_option");
        });

        modelBuilder.Entity<Product_Attribute_Value>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.AttributeId }).HasName("Product_Attribute_Value_pkey");

            entity.HasOne(d => d.Attribute).WithMany(p => p.Product_Attribute_Values)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pav_attribute");

            entity.HasOne(d => d.Option).WithMany(p => p.Product_Attribute_Values).HasConstraintName("fk_pav_option");

            entity.HasOne(d => d.Product).WithMany(p => p.Product_Attribute_Values)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pav_product");
        });

        modelBuilder.Entity<Product_Type>(entity =>
        {
            entity.HasKey(e => e.ProductTypeId).HasName("Product_Type_pkey");

            entity.Property(e => e.ProductTypeId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Category).WithMany(p => p.Product_Types)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_type_category");
        });

        modelBuilder.Entity<Refresh_Token>(entity =>
        {
            entity.HasKey(e => e.RefreshTokenId).HasName("Refresh_Token_pkey");

            entity.Property(e => e.RefreshTokenId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.RevokedAt).IsRequired(false);

            entity.HasOne(d => d.User).WithMany(p => p.Refresh_Tokens)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_refresh_user");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("Review_pkey");

            entity.Property(e => e.ReviewId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Order).WithOne(p => p.Review)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_orderid");

            entity.HasOne(d => d.Reviewee).WithMany(p => p.ReviewReviewees)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_revieweeid");

            entity.HasOne(d => d.Reviewer).WithMany(p => p.ReviewReviewers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_review_reviewerid");
        });

        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.HasKey(e => e.ShipmentId).HasName("Shipment_pkey");

            entity.Property(e => e.ShipmentId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.CollectionAppointment).WithMany(p => p.Shipments).HasConstraintName("fk_shipment_collection_appointment");

            entity.HasOne(d => d.Order).WithMany(p => p.Shipments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_shipment_orderid");
        });

        modelBuilder.Entity<Subscription_Package>(entity =>
        {
            entity.HasKey(e => e.PackageId).HasName("Subscription_Package_pkey");

            entity.Property(e => e.PackageId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("Users_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.IsEmailVerified).HasDefaultValue(false);
        });

        modelBuilder.Entity<User_Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("User_Subscription_pkey");

            entity.Property(e => e.SubscriptionId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Package).WithMany(p => p.User_Subscriptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_us_package");

            entity.HasOne(d => d.User).WithMany(p => p.User_Subscriptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_us_user");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.WalletId).HasName("Wallet_pkey");

            entity.Property(e => e.WalletId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Wallets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_user");
        });

        modelBuilder.Entity<Wallet_Ledger>(entity =>
        {
            entity.HasKey(e => e.LedgerId).HasName("Wallet_Ledger_pkey");

            entity.Property(e => e.LedgerId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Wallet_Ledgers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_ledger_walletid");

            entity.HasOne(d => d.WalletTransaction).WithMany(p => p.Wallet_Ledgers)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_wallet_ledger_wallettransactionid");
        });

        modelBuilder.Entity<Wallet_Transaction>(entity =>
        {
            entity.HasKey(e => e.WalletTransactionId).HasName("Wallet_Transaction_pkey");

            entity.Property(e => e.WalletTransactionId).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(d => d.FromWallet).WithMany(p => p.Wallet_TransactionFromWallets).HasConstraintName("fk_wallet_transaction_fromwalletid");

            entity.HasOne(d => d.Payment).WithMany(p => p.Wallet_Transactions).HasConstraintName("fk_wallet_transaction_paymentid");

            entity.HasOne(d => d.ToWallet).WithMany(p => p.Wallet_TransactionToWallets).HasConstraintName("fk_wallet_transaction_towalletid");
        });

        modelBuilder.Entity<Withdrawal>(entity =>
        {
            entity.HasKey(e => e.WithdrawalId).HasName("Withdrawal_pkey");

            entity.Property(e => e.WithdrawalId).ValueGeneratedNever();

            entity.HasOne(d => d.UserBank).WithMany(p => p.Withdrawals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_withdrawal_userbankid");

            entity.HasOne(d => d.Wallet).WithMany(p => p.Withdrawals)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_withdrawal_walletid");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
