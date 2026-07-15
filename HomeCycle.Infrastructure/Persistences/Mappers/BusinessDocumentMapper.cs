using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class BusinessDocumentMapper
    {
        public static business_document ToDomain(this Business_Document entity)
        {
            return new business_document
            {
                BusinessDocumentId = entity.BusinessDocumentId,
                BusinessProfileId = entity.BusinessProfileId,
                DocumentType = entity.DocumentType,
                DocumentUrl = entity.DocumentUrl,
                Status = entity.Status,
                VerifiedBy = entity.VerifiedBy,
                VerifiedAt = entity.VerifiedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                RejectReason = entity.RejectReason
            };
        }

        public static Business_Document ToInfrastructure(this business_document entity)
        {
            return new Business_Document
            {
                BusinessDocumentId = entity.BusinessDocumentId,
                BusinessProfileId = entity.BusinessProfileId,
                DocumentType = entity.DocumentType,
                DocumentUrl = entity.DocumentUrl,
                Status = entity.Status,
                VerifiedBy = entity.VerifiedBy,
                VerifiedAt = entity.VerifiedAt,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                RejectReason = entity.RejectReason
            };
        }
    }
}
