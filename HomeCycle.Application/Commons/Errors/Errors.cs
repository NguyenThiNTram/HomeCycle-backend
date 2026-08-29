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
        public static readonly Error EmailRequired = new("AUTH_EMAIL_REQUIRED", "Email is required.");

        public static readonly Error InvalidEmail = new("AUTH_EMAIL_INVALID", "Email format is invalid.");

        public static readonly Error FullNameRequired = new("AUTH_FULLNAME_REQUIRED", "Full name is required.");

        public static readonly Error InvalidFullName = new("AUTH_FULLNAME_INVALID", "Full name may contain letters and spaces only.");

        public static readonly Error UsernameRequired = new("AUTH_USERNAME_REQUIRED", "Username is required.");

        public static readonly Error InvalidUsername = new("AUTH_USERNAME_INVALID", "Username may contain letters, numbers, and underscores only.");

        public static readonly Error PasswordRequired = new("AUTH_PASSWORD_REQUIRED", "Password is required.");

        public static readonly Error InvalidPasswordLength = new("AUTH_PASSWORD_LENGTH_INVALID", "Password must be between 6 and 20 characters.");

        public static readonly Error PhoneNumberRequired = new("AUTH_PHONE_REQUIRED", "Phone number is required.");

        public static readonly Error InvalidPhoneNumber = new("AUTH_PHONE_INVALID", "Phone number must contain 9 or 10 digits and start with 0.");

        // AUTHENTICATION

        public static readonly Error InvalidCredential = new("AUTH_INVALID_CREDENTIAL","Invalid username or password.");

        public static readonly Error UserNotFound = new("AUTH_USER_NOT_FOUND", "User not found.");

        public static readonly Error InvalidRefreshToken = new("AUTH_INVALID_REFRESH_TOKEN", "Refresh token is invalid.");

        public static readonly Error ExpiredRefreshToken = new("AUTH_REFRESH_TOKEN_EXPIRED", "Refresh token has expired.");

        public static readonly Error RevokedRefreshToken = new("AUTH_REFRESH_TOKEN_REVOKED", "Refresh token has been revoked.");

        public static readonly Error EmailNotVerified = new("AUTH_EMAIL_NOT_VERIFIED", "Email has not been verified.");

        public static readonly Error InvalidOtp = new("AUTH_INVALID_OTP", "Invalid OTP.");

        // ACCOUNT STATUS

        public static readonly Error AccountSuspended = new("AUTH_ACCOUNT_SUSPENDED", "Account has been suspended.");

        public static readonly Error CannotLockSelf = new("AUTH_CANNOT_LOCK_SELF", "Admin cannot lock their own account.");

        public static readonly Error CannotLockAdmin = new("AUTH_CANNOT_LOCK_ADMIN", "Cannot lock another admin account.");

        public static readonly Error AlreadyLocked = new("AUTH_ACCOUNT_ALREADY_LOCKED", "Account is already locked.");

        public static readonly Error NotLocked = new("AUTH_ACCOUNT_NOT_LOCKED", "Account is not locked.");

        public static readonly Error AccountDeleted = new("AUTH_ACCOUNT_DELETED", "Account has been deleted.");

        // REGISTRATION

        public static readonly Error EmailExists = new("AUTH_EMAIL_EXISTS", "Email already exists.");

        public static readonly Error UsernameExists = new("AUTH_USERNAME_EXISTS", "Username already exists.");

        public static readonly Error InvalidRegistrationSession = new("AUTH_INVALID_REGISTRATION_SESSION", "The registration session is invalid or has expired.");
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

        public static Error OfferTermsChanged(decimal currentPrice, int currentQuantity)
            => new("OFFER_TERMS_CHANGED",
                   $"Đối phương vừa cập nhật đề nghị: giá {currentPrice:N0}đ, số lượng {currentQuantity}. " +
                   "Vui lòng xem lại trước khi xác nhận.");
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

        public static readonly Error OrderNotFound =
            new("DISPUTE_ORDER_NOT_FOUND", "Không tìm thấy đơn hàng.");

        public static readonly Error AgreementNotFound =
            new("DISPUTE_AGREEMENT_NOT_FOUND", "Không tìm thấy thỏa thuận của đơn hàng.");

        public static readonly Error Forbidden =
            new("DISPUTE_FORBIDDEN", "Bạn không có quyền thực hiện thao tác này trên đơn hàng hoặc tranh chấp.");

        public static readonly Error DuplicateActiveDispute =
            new("DISPUTE_ALREADY_ACTIVE", "Đơn hàng đang có một tranh chấp chưa được xử lý.");

        public static readonly Error InvalidOrderStatus =
            new("DISPUTE_INVALID_ORDER_STATUS", "Trạng thái hiện tại của đơn hàng không cho phép tạo tranh chấp.");

        public static readonly Error InvalidCompletionState =
            new("DISPUTE_INVALID_COMPLETION_STATE", "Không xác định được thời điểm hoàn tất hoặc giao hàng của đơn hàng.");

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
    }

    public static class OrderErrors
    {
        public static readonly Error NotFound =
            new("Order.NotFound", "Không tìm thấy đơn hàng.");

        public static readonly Error AgreementNotFound =
            new("Agreement.NotFound", "Không tìm thấy thỏa thuận của đơn hàng.");

        public static readonly Error Forbidden =
            new("Auth.Forbidden", "Bạn không có quyền thực hiện thao tác này trên đơn hàng.");

        public static readonly Error InvalidStatus =
            new("Order.InvalidStatus", "Trạng thái hiện tại của đơn hàng không cho phép thực hiện thao tác này.");

        public static readonly Error DeliveryMethodMissing =
            new("Order.DeliveryMethodMissing", "Không xác định được phương thức giao nhận của đơn hàng.");

        public static readonly Error DirectHandoverOnly =
            new("Order.DirectHandoverOnly", "Xác nhận bàn giao của Seller chỉ áp dụng cho giao nhận trực tiếp.");

        public static readonly Error CollectionAppointmentNotFound =
            new("Order.CollectionAppointmentNotFound", "Không tìm thấy lịch thu gom của đơn hàng.");

        public static readonly Error BothCheckInRequired =
            new("Order.BothCheckInRequired", "Buyer và Seller phải check-in lịch thu gom trước khi xác nhận giao nhận.");

        public static readonly Error ShipmentNotFound =
            new("Order.ShipmentNotFound", "Không tìm thấy thông tin vận chuyển của đơn hàng.");

        public static readonly Error ShipmentNotDelivered =
            new("Order.ShipmentNotDelivered", "Đơn vận chuyển chưa được xác nhận giao thành công.");
    }
}
