using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public static class TradingAccess
    {
        public static bool IsOfferSender(offer offer, Guid userId)
            => offer.SenderId == userId;

        public static bool IsOfferReceiver(offer offer, Guid userId)
            => offer.ReceiverId == userId;

        public static bool IsOfferParticipant(offer offer, Guid userId)
            => offer.SenderId == userId ||
               offer.ReceiverId == userId;

        public static bool IsNegotiationParticipant(
            negotiation negotiation,
            Guid userId)
            => negotiation.BuyerId == userId ||
               negotiation.SellerId == userId;

        public static bool CanRespondToProposal(
            negotiation negotiation,
            message proposal,
            Guid userId)
            => IsNegotiationParticipant(negotiation, userId)
               && proposal.NegotiationId == negotiation.NegotiationId
               && proposal.SenderId != userId
               && proposal.OfferStatus == MessageOfferStatus.Pending;
    }
}
