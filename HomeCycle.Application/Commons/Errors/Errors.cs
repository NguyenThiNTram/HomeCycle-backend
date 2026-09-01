using HomeCycle.Application.Commons.Results;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Errors
{
    public static class AuthErrors
    {
        public static readonly Error EmailExists = new("AUTH_EMAIL_EXISTS", "Email already exists.");

        public static readonly Error UsernameExists = new("AUTH_USERNAME_EXISTS", "Username already exists.");

        public static readonly Error InvalidCredential = new("AUTH_INVALID_CREDENTIAL","Invalid username or password.");

        public static readonly Error UserNotFound = new("AUTH_USER_NOT_FOUND", "User not found.");

        public static readonly Error InvalidRefreshToken = new("AUTH_INVALID_REFRESH_TOKEN", "Refresh token is invalid.");

        public static readonly Error ExpiredRefreshToken = new("AUTH_REFRESH_TOKEN_EXPIRED", "Refresh token has expired.");

        public static readonly Error RevokedRefreshToken = new("AUTH_REFRESH_TOKEN_REVOKED", "Refresh token has been revoked.");

        public static readonly Error EmailNotVerified = new("AUTH_EMAIL_NOT_VERIFIED", "Email has not been verified.");

        public static readonly Error InvalidOtp = new("AUTH_INVALID_OTP", "Invalid OTP.");

        public static readonly Error AccountSuspended = new("AUTH_ACCOUNT_SUSPENDED", "Account has been suspended.");

        public static readonly Error CannotLockSelf = new("AUTH_CANNOT_LOCK_SELF", "Admin cannot lock their own account.");

        public static readonly Error CannotLockAdmin = new("AUTH_CANNOT_LOCK_ADMIN", "Cannot lock another admin account.");

        public static readonly Error AlreadyLocked = new("AUTH_ACCOUNT_ALREADY_LOCKED", "Account is already locked.");

        public static readonly Error NotLocked = new("AUTH_ACCOUNT_NOT_LOCKED", "Account is not locked.");
    }

    public static class ProfileErrors
    {
        public static readonly Error UserNotFound = new("AUTH_USER_NOT_FOUND", "User not found! Please try again.");
        public static readonly Error ProfileNotFound = new("AUTH_PROFILE_NOT_FOUND", "Profile not found! Please try again.");
    }

    public static class CategoryErrors
    {
        public static readonly Error CategoryNotFound = new("CATEGORY_NOT_FOUND", "Category not found.");

        public static readonly Error CategoryAlreadyExists = new("CATEGORY_ALREADY_EXISTS", "Category name already exists.");

        public static readonly Error CategoryInactive = new("CATEGORY_INACTIVE", "Category has been deactivated.");
    }

    public static class BrandErrors
    {
        public static readonly Error BrandNotFound = new("BRAND_NOT_FOUND", "Brand does not exist.");

        public static readonly Error BrandAlreadyExists = new("BRAND_ALREADY_EXISTS", "Brand already exists.");
    }

    public static class ProductTypeErrors
    {
        public static readonly Error ProductTypeNotFound = new("PRODUCT_TYPE_NOT_FOUND", "Product type does not exist.");

        public static readonly Error ProductTypeAlreadyExists = new("PRODUCT_TYPE_ALREADY_EXISTS", "Product type already exists.");

        public static readonly Error CategoryNotFound = new("CATEGORY_NOT_FOUND", "Category does not exist.");

        public static readonly Error AttributeAlreadyExists = new("ATTRIBUTE_ALREADY_EXISTS", "Attribute already exists.");
        public static readonly Error AttributeNotFound = new("ATTRIBUTE_NOT_FOUND", "Attribute does not exist.");
        public static readonly Error AttributeInUse = new("ATTRIBUTE_ALREADY_IN_USE", "Attribute already in use.");
        public static readonly Error CannotChangeDataTypeInUse = new("DATA_TYPE_CANNOT_CHANGE_IN_USE", "Cannot change the data because in used");
        public static readonly Error CannotChangeInputModeInUse = new("INPUT_MODE_CANNOT_CHANGE_IN_USE", "Cannot change the Input Mode because in used");

    }

    public static class ProductAttributeErrors
    {
        public static readonly Error AttributeNotFound = new("ATTRIBUTE_NOT_FOUND", "Product attribute does not exist.");

        public static readonly Error AttributeAlreadyExists = new("ATTRIBUTE_ALREADY_EXISTS", "Attribute already exists in this product type.");

        public static readonly Error RequiredAttributeMissing = new("ATTRIBUTE_REQUIRED_MISSING", "Required attribute is missing.");

    }

    public static class ProductAttributeOptionErrors
    {
        public static readonly Error OptionNotFound = new("ATTRIBUTE_OPTION_NOT_FOUND", "Attribute option does not exist.");

        public static readonly Error OptionAlreadyExists = new("ATTRIBUTE_OPTION_ALREADY_EXISTS", "Attribute option already exists.");
        public static readonly Error OptionInUse = new("ATTRIBUTE_OPTION_ALREADY_IN_USE", "Attribute option already in use.");
    }

    public static class ProductErrors
    {
        public static readonly Error ProductNotFound = new("PRODUCT_NOT_FOUND", "Product not found.");

        public static readonly Error InvalidCategory = new("PRODUCT_INVALID_CATEGORY", "Invalid product category.");

        public static readonly Error InvalidProductType = new("PRODUCT_INVALID_PRODUCT_TYPE", "Invalid product type for the category.");
        public static readonly Error InvalidBrand = new("PRODUCT_INVALID_BRAND", "Invalid brand.");
    }

    public static class PostErrors
    {
        public static readonly Error NotFound = new("POST_NOT_FOUND", "The post is not found.");

        public static readonly Error InvalidPostType = new("POST_INVALID_TYPE", "Invalid post type.");

        public static readonly Error UnauthorizedOwner = new("POST_UNAUTHORIZED", "You do not have permission to perform this action on the post.");

        public static readonly Error PostAlreadyClosedOrDeleted = new("POST_ALREADY_CLOSED_OR_DELETED", "The post is already closed or deleted.");

        public static readonly Error PostAlreadySuspended = new("POST_ALREADY_SUSPENDED", "The post is already suspended.");

        public static readonly Error Forbidden = new("POST_FORBIDDEN", "You do not have permission to access this post.");

        public static readonly Error PostExpired = new("POST_EXPIRED", "Bài đăng đã hết thời hạn cho phép chỉnh sửa.");

        public static readonly Error RoleNotAllowed = new("POST_ROLE_NOT_ALLOWED", "Your account role is not allowed to create this type of post.");

        public static Error InvalidUpdateQuantity(int soldQuantity, int requestedQuantity)
            => new(
                "POST_INVALID_QUANTITY",
                $"Số lượng cập nhật ({requestedQuantity}) không thể nhỏ hơn số lượng đã bán/giao dịch ({soldQuantity}).");
    }

    public static class OfferErrors
    {
        public static readonly Error NotFound = new("OFFER_NOT_FOUND", "The offer is not found.");

        public static readonly Error Forbidden = new("OFFER_FORBIDDEN", "You do not have permission to access this offer.");

        public static readonly Error NotPending = new("OFFER_NOT_PENDING", "The offer is no longer in pending state.");

        public static readonly Error PostNotFound = new("OFFER_POST_NOT_FOUND", "The post does not exist.");

        public static readonly Error PostNotActive = new("OFFER_POST_NOT_ACTIVE", "The post is not active.");

        public static readonly Error InvalidQuantity = new("OFFER_INVALID_QUANTITY", "Offer quantity must be greater than zero.");

        public static readonly Error CannotOfferOwnPost = new("OFFER_CANNOT_OFFER_OWN_POST", "You cannot send an offer for your own post.");

        public static readonly Error DuplicatePending = new("OFFER_DUPLICATE_PENDING", "You already have a pending offer for this post.");

        public static readonly Error RoleNotAllowed = new("OFFER_ROLE_NOT_ALLOWED", "Your account role is not allowed to make this offer.");

        public static readonly Error BusinessCannotOfferBuyPost = new("OFFER_B2B_NOT_ALLOWED", "Business accounts cannot offer on a business buy post.");
        public static readonly Error UserNotActive = new("OFFER_USER_NOT_ACTIVE", "Your account is not active. Please verify your email or contact support.");
        public static Error PriceOutOfRange(decimal minPrice, decimal maxPrice)
            => new("OFFER_PRICE_OUT_OF_RANGE",
                   $"Offer price must be between {minPrice:N0} and {maxPrice:N0}.");

        public static Error QuantityExceedsRemaining(int requested, int remaining)
            => new("OFFER_QUANTITY_EXCEEDS_REMAINING",
                   $"Offer quantity ({requested}) exceeds the remaining quantity ({remaining}).");
    }

    public static class NegotiationErrors
    {
        public static readonly Error NotFound = new("NEGOTIATION_NOT_FOUND", "The negotiation is not found.");

        public static readonly Error NotOpen = new("NEGOTIATION_NOT_OPEN", "The negotiation is not in open state.");

        public static readonly Error Forbidden = new("NEGOTIATION_FORBIDDEN", "You do not have permission to access this negotiation.");

        public static readonly Error InvalidStatusForCounter = new("NEGOTIATION_INVALID_STATUS_FOR_COUNTER", "You can only counter an offer when the negotiation is in open state.");
        public static readonly Error AlreadyExists = new("NEGOTIATION_ALREADY_EXISTS", "A negotiation already exists for this offer.");
        public static readonly Error ProposalNotFound = new("NEGOTIATION_PROPOSAL_NOT_FOUND", "The proposal message is not found.");
        public static readonly Error InvalidStatusForCancel = new("NEGOTIATION_INVALID_STATUS_FOR_CANCEL", "You can only cancel a negotiation that is still in open or agreed state.");
    }

    public static class MessageErrors
    {
        public static readonly Error NotFound = new("MESSAGE_NOT_FOUND", "The message is not found.");
        public static readonly Error Forbidden = new("MESSAGE_FORBIDDEN", "You do not have permission to access this message.");
        public static readonly Error ClientMessageIdConflict = new("MESSAGE_CLIENT_ID_CONFLICT", "A message with the same client message ID already exists.");
        public static readonly Error InvalidMessage = new("MESSAGE_INVALID", "The message is invalid or cannot be processed.");
        public static readonly Error NegotiationReadOnly = new("MESSAGE_NEGOTIATION_READ_ONLY", "Cannot send messages in a negotiation that is not open.");
    }

    public static class CartErrors
    {
        public static readonly Error ItemNotFound = new("CART_ITEM_NOT_FOUND", "The cart item is not found.");

        public static readonly Error ItemExists = new("CART_ITEM_EXISTS", "The post is already in your cart.");

        public static readonly Error PostNotFound = new("CART_POST_NOT_FOUND", "The post does not exist.");

        public static readonly Error PostNotActive = new("CART_POST_NOT_ACTIVE", "The post is not active.");

        public static readonly Error CannotAddOwnPost = new("CART_CANNOT_ADD_OWN_POST", "You cannot add your own post to the cart.");

        public static readonly Error InvalidQuantity = new("CART_INVALID_QUANTITY", "Quantity must be greater than zero.");

        public static Error QuantityExceedsRemaining(int requested, int remaining)
            => new("CART_QUANTITY_EXCEEDS_REMAINING",
                   $"Quantity ({requested}) exceeds the remaining quantity ({remaining}).");

        public static readonly Error Forbidden = new("CART_FORBIDDEN", "You do not have permission to access this cart item.");
    }

    public static class DisputeErrors
    {
        public static readonly Error NotFound =
            new("DISPUTE_NOT_FOUND", "Không tìm thấy tranh chấp.");

        public static readonly Error Forbidden =
            new("DISPUTE_FORBIDDEN", "Bạn không có quyền thực hiện thao tác này trên đơn hàng hoặc tranh chấp.");

        public static readonly Error DuplicateActiveDispute =
            new("DISPUTE_ALREADY_ACTIVE", "Đơn hàng đang có một tranh chấp chưa được xử lý.");

        public static readonly Error MissingTarget =
            new("DISPUTE_TARGET_MISSING", "Tranh chấp không xác định được đối tượng liên quan.");

        public static readonly Error SenderNotFound =
            new("DISPUTE_SENDER_NOT_FOUND", "Không tìm thấy người gửi tranh chấp.");

        public static Error WindowExpired(DateTime deadline) =>
            new("DISPUTE_WINDOW_EXPIRED", $"Thời hạn tạo tranh chấp đã kết thúc lúc {deadline:O}.");

        public static Error UnsupportedTarget(DisputeTargetType targetType) =>
            new("DISPUTE_TARGET_NOT_SUPPORTED", $"Loại đối tượng tranh chấp '{targetType}' hiện chưa được hỗ trợ.");

        public static Error InvalidCategory(DisputeCategory category) =>
            new("DISPUTE_INVALID_CATEGORY", $"Loại tranh chấp '{category}' không phù hợp với tranh chấp đơn hàng.");

        public static readonly Error CloseNotAllowed =
            new("DISPUTE_CLOSE_NOT_ALLOWED", "Chỉ có thể đóng tranh chấp đang ở trạng thái chờ xử lý.");

        public static readonly Error AlreadyUnderReview =
            new("DISPUTE_ALREADY_UNDER_REVIEW", "Tranh chấp đã được Moderator tiếp nhận và không thể tự đóng.");

    }

    public static class OrderErrors
    {
        public static readonly Error NotFound =
            new("Order.NotFound", "Không tìm thấy đơn hàng.");

        public static readonly Error NotCreated =
            new("Order.NotCreated", "Thỏa thuận chưa phát sinh đơn hàng do chưa thanh toán thành công.");

        public static readonly Error Forbidden =
            new("Order.Forbidden", "Bạn không có quyền thực hiện thao tác này trên đơn hàng.");

        public static readonly Error InvalidStatus =
            new("Order.InvalidStatus", "Trạng thái hiện tại của đơn hàng không cho phép thực hiện thao tác này.");

        public static readonly Error DeliveryMethodMissing =
            new("Order.DeliveryMethodMissing", "Không xác định được phương thức giao nhận của đơn hàng.");

        public static readonly Error DirectHandoverOnly =
            new("Order.DirectHandoverOnly", "Xác nhận bàn giao của Seller chỉ áp dụng cho giao nhận trực tiếp.");

        public static readonly Error ShipmentNotFound =
            new("Order.ShipmentNotFound", "Không tìm thấy thông tin vận chuyển của đơn hàng.");

        public static readonly Error ShipmentNotDelivered =
            new("Order.ShipmentNotDelivered", "Đơn vận chuyển chưa được xác nhận giao thành công.");
        public static readonly Error CancellationRequiresRejectedInspection =
            new("Order.CancellationRequiresRejectedInspection", "Chỉ có thể hủy đơn theo luồng này sau khi Seller từ chối kết quả kiểm định.");

        public static readonly Error ActiveDisputeBlocksCancellation =
            new("Order.ActiveDisputeBlocksCancellation", "Đơn hàng đang có tranh chấp nên không thể hủy trực tiếp.");

        public static readonly Error InvalidCompletionState =
            new("Order.InvalidCompletionState", "Đơn hàng đã hoàn tất nhưng không xác định được thời điểm hoàn tất hoặc giao hàng.");

        public static readonly Error NotDisputing =
            new(
                "Order.NotDisputing",
                "Đơn hàng hiện không ở trạng thái tranh chấp.");
    }

    public static class PlatformPolicyErrors
    {
        public static Error ActiveNotFound(PlatformPolicyType policyType) =>
            new("PlatformPolicy.ActiveNotFound", $"Không tìm thấy policy đang hoạt động cho '{policyType}'.");

        public static Error InvalidContent(PlatformPolicyType policyType) =>
            new("PlatformPolicy.InvalidContent", $"Nội dung cấu hình của policy '{policyType}' không hợp lệ.");

        public static readonly Error InvalidDisputePolicy =
            new("PlatformPolicy.InvalidDisputePolicy", "Thời gian dispute của tài khoản uy tín thấp không được ngắn hơn thời gian dispute thông thường.");

        public static readonly Error InvalidAppointmentPolicy =
            new("PlatformPolicy.InvalidAppointmentPolicy", "Thời hạn yêu cầu đổi lịch phải lớn hơn hoặc bằng thời hạn được phép hủy lịch.");
        public static Error VersionNotFound(PlatformPolicyType policyType, int version) =>
            new("PlatformPolicy.VersionNotFound", $"Không tìm thấy version {version} của policy '{policyType}'.");

        public static readonly Error VersionAlreadyActive =
            new("PlatformPolicy.VersionAlreadyActive", "Version này hiện đang là version được áp dụng.");

        public static Error UnsupportedType(string policyType) =>
            new("PlatformPolicy.UnsupportedType", $"Policy type '{policyType}' không được hệ thống hỗ trợ.");
    }

    public static class AgreementErrors
    {
        public static readonly Error NotFound =
            new("Agreement.NotFound", "Không tìm thấy thỏa thuận.");

        public static readonly Error AlreadyExists =
            new("Agreement.AlreadyExists", "Thỏa thuận đã tồn tại.");

        public static readonly Error Forbidden =
            new("Agreement.Forbidden", "Bạn không có quyền thực hiện thao tác này trên thỏa thuận.");

        public static readonly Error InvalidStatus =
            new("Agreement.InvalidStatus", "Trạng thái hiện tại của thỏa thuận không cho phép thực hiện thao tác này.");

        public static readonly Error AlreadyConfirmed =
            new("Agreement.AlreadyConfirmed", "Bạn đã xác nhận thỏa thuận này rồi.");

        public static readonly Error RevisionMismatch =
            new("Agreement.RevisionMismatch", "Nội dung thỏa thuận vừa được cập nhật. Vui lòng tải lại và xem nội dung mới nhất trước khi xác nhận.");

        public static readonly Error OnlySellerCanCreate =
            new("Agreement.OnlySellerCanCreate", "Chỉ người bán mới có quyền tạo thỏa thuận.");
    }

    public static class AppointmentErrors
    {
        public static readonly Error NotFound =
            new("Appointment.NotFound", "Không tìm thấy lịch hẹn.");

        public static readonly Error Forbidden =
            new("Appointment.Forbidden", "Bạn không có quyền thực hiện thao tác này trên lịch hẹn.");

        public static readonly Error InvalidType =
            new("Appointment.InvalidType", "Loại lịch hẹn không hợp lệ.");

        public static readonly Error InvalidStatus =
            new("Appointment.InvalidStatus", "Trạng thái hiện tại của lịch hẹn không cho phép thực hiện thao tác này.");

        public static readonly Error InspectionDetailNotFound =
            new("Appointment.InspectionDetailNotFound", "Không tìm thấy thông tin lịch kiểm định.");

        public static readonly Error CollectionDetailNotFound =
            new("Appointment.CollectionDetailNotFound", "Không tìm thấy thông tin lịch thu gom.");

        public static readonly Error ScheduleMissing =
            new("Appointment.ScheduleMissing", "Không xác định được thời gian của lịch hẹn.");

        public static readonly Error Cancelled =
            new("Appointment.Cancelled", "Lịch hẹn đã bị hủy.");

        public static readonly Error AlreadyCompleted =
            new("Appointment.AlreadyCompleted", "Lịch hẹn đã hoàn tất.");

        public static readonly Error UnsupportedAction =
            new("Appointment.UnsupportedAction", "Lịch hẹn này không hỗ trợ thao tác trực tiếp của người dùng.");

        public static readonly Error CheckInInspectionOnly =
            new("Appointment.CheckInInspectionOnly", "Check-in chỉ áp dụng cho lịch kiểm định.");

        public static readonly Error CheckInAlreadyStarted =
            new("Appointment.CheckInAlreadyStarted", "Không thể thay đổi lịch sau khi một trong hai bên đã check-in.");

        public static readonly Error PendingRescheduleExists =
            new("Appointment.PendingRescheduleExists", "Lịch hẹn đang có một yêu cầu đổi lịch chưa được phản hồi.");

        public static readonly Error InvalidRescheduleProposal =
            new("Appointment.InvalidRescheduleProposal", "Yêu cầu đổi lịch không hợp lệ.");

        public static readonly Error CannotRespondOwnReschedule =
            new("Appointment.CannotRespondOwnReschedule", "Người gửi yêu cầu đổi lịch không thể tự chấp nhận hoặc từ chối yêu cầu của mình.");

        public static readonly Error RescheduleProposalExpired =
            new("Appointment.RescheduleProposalExpired", "Thời gian được đề xuất cho lịch mới đã qua.");

        public static readonly Error SameSchedule =
            new("Appointment.SameSchedule", "Thời gian đề xuất mới phải khác lịch hiện tại.");

        public static Error CheckInNotOpen(DateTime openAt) =>
            new("Appointment.CheckInNotOpen", $"Check-in chưa mở. Có thể check-in từ {openAt:O}.");

        public static Error RescheduleCutoffPassed(DateTime cutoff) =>
            new("Appointment.RescheduleCutoffPassed", $"Đã quá thời hạn yêu cầu đổi lịch. Hạn cuối: {cutoff:O}.");

        public static Error CancellationCutoffPassed(DateTime cutoff) =>
            new("Appointment.CancellationCutoffPassed", $"Đã quá thời hạn hủy lịch. Hạn cuối: {cutoff:O}.");

        public static Error CollectionConfirmationNotOpen(DateTime scheduledAt) =>
            new("Appointment.CollectionConfirmationNotOpen", $"Chưa đến ngày được phép xác nhận giao nhận. Lịch thu gom: {scheduledAt:O}.");
    }

    public static class InspectionErrors
    {
        public static readonly Error NotFound =
            new("Inspection.NotFound", "Không tìm thấy biểu mẫu kiểm định.");

        public static readonly Error AlreadyExists =
            new("Inspection.AlreadyExists", "Lịch kiểm định này đã có biểu mẫu kiểm định.");

        public static readonly Error BuyerOnly =
            new("Inspection.BuyerOnly", "Chỉ Buyer thực hiện kiểm định mới có quyền thao tác biểu mẫu.");

        public static readonly Error SellerOnly =
            new("Inspection.SellerOnly", "Chỉ Seller của giao dịch mới có quyền xác nhận kết quả kiểm định.");

        public static readonly Error InvalidAppointment =
            new("Inspection.InvalidAppointment", "Lịch hẹn này không phải lịch kiểm định hợp lệ.");

        public static readonly Error AppointmentNotInProgress =
            new("Inspection.AppointmentNotInProgress", "Biểu mẫu chỉ được xử lý khi lịch kiểm định đang diễn ra.");

        public static readonly Error BothCheckInRequired =
            new("Inspection.BothCheckInRequired", "Buyer và Seller phải check-in trước khi tiến hành kiểm định.");

        public static readonly Error DraftOnly =
            new("Inspection.DraftOnly", "Chỉ biểu mẫu Draft mới được chỉnh sửa hoặc gửi.");

        public static readonly Error PendingConfirmationOnly =
            new("Inspection.PendingConfirmationOnly", "Biểu mẫu không ở trạng thái chờ Seller xác nhận.");

        public static readonly Error RevisionMismatch =
            new("Inspection.RevisionMismatch", "Biểu mẫu kiểm định đã được cập nhật. Vui lòng tải lại phiên bản mới nhất.");

        public static readonly Error Incomplete =
            new("Inspection.Incomplete", "Vui lòng hoàn thành đầy đủ checklist và kết luận kiểm định trước khi gửi.");

        public static readonly Error SuggestedPriceRequired =
            new("Inspection.SuggestedPriceRequired", "Kết luận điều chỉnh giá yêu cầu nhập giá mới.");

        public static readonly Error SuggestedPriceUnchanged =
            new("Inspection.SuggestedPriceUnchanged", "Giá đề xuất mới phải khác giá giao dịch hiện tại.");

        public static readonly Error InvalidOrderPrice =
            new("Inspection.InvalidOrderPrice", "Không xác định được giá giao dịch hiện tại.");

        public static readonly Error AcceptedRequired =
            new("Inspection.AcceptedRequired", "Biểu mẫu phải được Seller xác nhận trước khi tiếp tục thu gom.");

        public static readonly Error FailedCannotCollect =
            new("Inspection.FailedCannotCollect", "Kết quả kiểm định không đạt nên không thể tiếp tục thu gom.");

        public static readonly Error CollectActionAlreadySelected =
            new("Inspection.CollectActionAlreadySelected", "Phương án thu gom đã được lựa chọn.");

        public static readonly Error DepositMissing =
            new("Inspection.DepositMissing", "Không xác định được khoản tiền cọc cần hoàn.");
    }

    public static class PaymentErrors
    {
        public static readonly Error RefundPaymentNotFound =
            new("Payment.RefundPaymentNotFound", "Không tìm thấy giao dịch thanh toán để thực hiện hoàn tiền.");

        public static readonly Error RefundWalletNotFound =
            new("Payment.RefundWalletNotFound", "Không tìm thấy ví cần thiết để thực hiện hoàn tiền.");

        public static readonly Error InsufficientHeldBalance =
            new("Payment.InsufficientHeldBalance", "Số dư đang tạm giữ không đủ để thực hiện hoàn tiền.");

        public static readonly Error InvalidRefundAmount =
            new("Payment.InvalidRefundAmount", "Số tiền hoàn không hợp lệ.");

        public static readonly Error AlreadyRefunded =
            new("Payment.AlreadyRefunded", "Khoản thanh toán đã được hoàn toàn bộ.");
    }

}
