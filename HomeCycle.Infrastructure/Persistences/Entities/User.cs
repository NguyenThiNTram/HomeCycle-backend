using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Table("Users")]
public partial class User
{
    [Key]
    public Guid UserId { get; set; }

    [StringLength(100)]
    public string Username { get; set; } = null!;

    [StringLength(255)]
    public string Email { get; set; } = null!;

    public bool IsEmailVerified { get; set; }

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? AvatarUrl { get; set; }

    public int Role { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Buyer")]
    public virtual ICollection<Agreement_Form> Agreement_FormBuyers { get; set; } = new List<Agreement_Form>();

    [InverseProperty("Seller")]
    public virtual ICollection<Agreement_Form> Agreement_FormSellers { get; set; } = new List<Agreement_Form>();

    [InverseProperty("User")]
    public virtual ICollection<Audit_Log> Audit_Logs { get; set; } = new List<Audit_Log>();

    [InverseProperty("User")]
    public virtual ICollection<Bank_Account> Bank_Accounts { get; set; } = new List<Bank_Account>();

    [InverseProperty("VerifiedByNavigation")]
    public virtual ICollection<Business_Document> Business_Documents { get; set; } = new List<Business_Document>();

    [InverseProperty("User")]
    public virtual Business_Profile? Business_Profile { get; set; }

    [InverseProperty("Moderator")]
    public virtual ICollection<Dispute> DisputeModerators { get; set; } = new List<Dispute>();

    [InverseProperty("Sender")]
    public virtual ICollection<Dispute> DisputeSenders { get; set; } = new List<Dispute>();

    [InverseProperty("TargetUser")]
    public virtual ICollection<Dispute> DisputeTargetUsers { get; set; } = new List<Dispute>();

    [InverseProperty("Inspector")]
    public virtual ICollection<Inspection_Form> Inspection_Forms { get; set; } = new List<Inspection_Form>();

    [InverseProperty("Sender")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [InverseProperty("Buyer")]
    public virtual ICollection<Negotiation> NegotiationBuyers { get; set; } = new List<Negotiation>();

    [InverseProperty("Seller")]
    public virtual ICollection<Negotiation> NegotiationSellers { get; set; } = new List<Negotiation>();

    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("User")]
    public virtual ICollection<OTP> OTPs { get; set; } = new List<OTP>();

    [InverseProperty("Receiver")]
    public virtual ICollection<Offer> OfferReceivers { get; set; } = new List<Offer>();

    [InverseProperty("Sender")]
    public virtual ICollection<Offer> OfferSenders { get; set; } = new List<Offer>();

    [InverseProperty("User")]
    public virtual ICollection<Payment_Transaction> Payment_Transactions { get; set; } = new List<Payment_Transaction>();

    [InverseProperty("Payer")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("User")]
    public virtual Personal_Profile? Personal_ProfileUser { get; set; }

    [InverseProperty("VerifiedByNavigation")]
    public virtual ICollection<Personal_Profile> Personal_ProfileVerifiedByNavigations { get; set; } = new List<Personal_Profile>();

    [InverseProperty("User")]
    public virtual ICollection<Refresh_Token> Refresh_Tokens { get; set; } = new List<Refresh_Token>();

    [InverseProperty("Reviewee")]
    public virtual ICollection<Review> ReviewReviewees { get; set; } = new List<Review>();

    [InverseProperty("Reviewer")]
    public virtual ICollection<Review> ReviewReviewers { get; set; } = new List<Review>();

    [InverseProperty("User")]
    public virtual ICollection<User_Subscription> User_Subscriptions { get; set; } = new List<User_Subscription>();

    [InverseProperty("User")]
    public virtual ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
}
