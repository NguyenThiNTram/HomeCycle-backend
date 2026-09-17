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

        public const string BankAccountChange = "BANK_ACCOUNT.CHANGE";

        public const string PaymentInitiate = "PAYMENT.INITIATE";
        public const string PaymentComplete = "PAYMENT.COMPLETE";
        public const string PaymentReconcile = "PAYMENT.RECONCILE";
        public const string PaymentRefund = "PAYMENT.REFUND";
        public const string PaymentRelease = "PAYMENT.RELEASE";

        public const string WithdrawalRequest = "WITHDRAWAL.REQUEST";
        public const string WithdrawalApprove = "WITHDRAWAL.APPROVE";
        public const string WithdrawalReject = "WITHDRAWAL.REJECT";
        public const string WithdrawalComplete = "WITHDRAWAL.COMPLETE";
        public const string WithdrawalRevert = "WITHDRAWAL.REVERT";

        public const string AgreementCreate = "AGREEMENT.CREATE";
        public const string AgreementUpdate = "AGREEMENT.UPDATE";
        public const string AgreementConfirm = "AGREEMENT.CONFIRM";
        public const string AgreementReopen = "AGREEMENT.REOPEN";

        public const string OrderHandoverConfirm = "ORDER.HANDOVER_CONFIRM";
        public const string OrderComplete = "ORDER.COMPLETE";
        public const string OrderCancel = "ORDER.CANCEL";
        public const string OrderReturnConfirm = "ORDER.RETURN_CONFIRM";
        public const string OrderReturnComplete = "ORDER.RETURN_COMPLETE";

        public const string AppointmentCheckIn = "APPOINTMENT.CHECK_IN";
        public const string AppointmentRescheduleRequest = "APPOINTMENT.RESCHEDULE_REQUEST";
        public const string AppointmentRescheduleAccept = "APPOINTMENT.RESCHEDULE_ACCEPT";
        public const string AppointmentRescheduleReject = "APPOINTMENT.RESCHEDULE_REJECT";
        public const string AppointmentCancel = "APPOINTMENT.CANCEL";

        public const string InspectionSubmit = "INSPECTION.SUBMIT";
        public const string InspectionConfirm = "INSPECTION.CONFIRM";
        public const string InspectionReject = "INSPECTION.REJECT";
        public const string InspectionCollectNow = "INSPECTION.COLLECT_NOW";

        public const string InspectionScheduleCollection = "INSPECTION.SCHEDULE_COLLECTION";

        public const string ShipmentSellerReady = "SHIPMENT.SELLER_READY";

        public const string DisputeCreate = "DISPUTE.CREATE";
        public const string DisputeClose = "DISPUTE.CLOSE";
        public const string DisputeClaim = "DISPUTE.CLAIM";
        public const string DisputeResolve = "DISPUTE.RESOLVE";
        public const string DisputeReject = "DISPUTE.REJECT";
        public const string DisputeReturnVerify = "DISPUTE.RETURN_VERIFY";

        public const string SubscriptionPackageCreate = "SUBSCRIPTION_PACKAGE.CREATE";
        public const string SubscriptionPackageUpdate = "SUBSCRIPTION_PACKAGE.UPDATE";
        public const string SubscriptionPackageUpdateStatus = "SUBSCRIPTION_PACKAGE.UPDATE_STATUS";
    }
}
