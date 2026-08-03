using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure;

[Index("BusinessProfileId", Name = "idx_business_document_profile")]
public partial class Business_Document
{
    [Key]
    public Guid BusinessDocumentId { get; set; }

    public Guid BusinessProfileId { get; set; }

    [StringLength(100)]
    public int DocumentType { get; set; }

    [StringLength(500)]
    public string DocumentUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [ForeignKey("BusinessProfileId")]
    [InverseProperty("Business_Documents")]
    public virtual Business_Profile BusinessProfile { get; set; } = null!;
}
