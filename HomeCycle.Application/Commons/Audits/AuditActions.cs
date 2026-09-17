using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Audits
{
    public static class AuditActions
    {
        public const string AuthLogin = "AUTH.LOGIN";
        public const string AuthRegister = "AUTH.REGISTER";
        public const string AuthModeratorVerifyEmail = "AUTH.MODERATOR_VERIFY_EMAIL";
        public const string AuthModeratorSetPassword = "AUTH.MODERATOR_SET_PASSWORD";

        public const string UserCreateModerator = "USER.CREATE_MODERATOR";
        public const string UserLock = "USER.LOCK";
        public const string UserUnlock = "USER.UNLOCK";

        public const string BusinessProfileSubmit = "BUSINESS_PROFILE.SUBMIT";
        public const string BusinessProfileUpdateDocuments = "BUSINESS_PROFILE.UPDATE_DOCUMENTS";
        public const string BusinessProfileUpdateIdentity = "BUSINESS_PROFILE.UPDATE_IDENTITY";
        public const string BusinessProfileUpdateRegistration = "BUSINESS_PROFILE.UPDATE_REGISTRATION";
        public const string BusinessProfileReview = "BUSINESS_PROFILE.REVIEW";
        public const string PersonalProfileReviewIdentity = "PERSONAL_PROFILE.REVIEW_IDENTITY";

        public const string PostCreate = "POST.CREATE";
        public const string PostUpdate = "POST.UPDATE";
        public const string PostClose = "POST.CLOSE";
        public const string PostReactivate = "POST.REACTIVATE";
        public const string PostDelete = "POST.DELETE";
        public const string PostSuspend = "POST.SUSPEND";

        public const string OfferCreate = "OFFER.CREATE";
        public const string OfferUpdate = "OFFER.UPDATE";
        public const string OfferCancel = "OFFER.CANCEL";
        public const string OfferReject = "OFFER.REJECT";
        public const string OfferAccept = "OFFER.ACCEPT";
        public const string OfferCounter = "OFFER.COUNTER";

        public const string NegotiationCounter = "NEGOTIATION.COUNTER";
        public const string NegotiationAccept = "NEGOTIATION.ACCEPT";
        public const string NegotiationReject = "NEGOTIATION.REJECT";
        public const string NegotiationCancel = "NEGOTIATION.CANCEL";
    }
}
