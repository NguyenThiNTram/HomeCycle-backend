using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Domain.Entities;
public class business_document
{
    public Guid BusinessDocumentId { get; set; }
    public Guid BusinessProfileId { get; set; }

    public int DocumentType { get; set; }
    public string DocumentUrl { get; set; }

    public int Status { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? RejectReason { get; set; }

    public business_document()
    {
    }

    public business_document(Guid BusinessDocumentId, Guid BusinessProfileId)
    {
        this.BusinessDocumentId = BusinessDocumentId;
        this.BusinessProfileId = BusinessProfileId;
    }

}
