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
        public static readonly Error PasswordResetEmailNotFound = new("AUTH_PASSWORD_RESET_EMAIL_NOT_FOUND", "Email chưa được đăng ký trong hệ thống.");
        public static readonly Error PasswordResetUnavailable = new("AUTH_PASSWORD_RESET_UNAVAILABLE", "Chỉ tài khoản cá nhân hoặc doanh nghiệp không bị khóa hoặc xóa mới được đặt lại mật khẩu.");
        public static readonly Error InvalidPasswordResetOtp = new("AUTH_PASSWORD_RESET_OTP_INVALID", "OTP không hợp lệ, đã hết hạn hoặc đã được sử dụng.");
        public static readonly Error PasswordResetOtpLocked = new("AUTH_PASSWORD_RESET_OTP_LOCKED", "Bạn đã nhập sai OTP quá nhiều lần. Vui lòng thử lại sau 15 phút.");
        public static readonly Error ModeratorCreationForbidden = new("AUTH_MODERATOR_CREATION_FORBIDDEN", "Chỉ quản trị viên đang hoạt động mới có thể tạo tài khoản kiểm duyệt viên.");
        public static readonly Error InvalidModeratorToken = new("AUTH_MODERATOR_TOKEN_INVALID", "Liên kết kích hoạt hoặc phiên thiết lập mật khẩu không hợp lệ, đã hết hạn hoặc đã được sử dụng.");
        public static readonly Error ModeratorActivationUnavailable = new("AUTH_MODERATOR_ACTIVATION_UNAVAILABLE", "Không thể kích hoạt tài khoản kiểm duyệt viên này.");
        public static readonly Error ModeratorEmailFailed = new("AUTH_MODERATOR_EMAIL_FAILED", "Tài khoản chờ kích hoạt đã được tạo nhưng không thể gửi email xác nhận.");
        public static readonly Error ModeratorConfigurationInvalid = new("AUTH_MODERATOR_CONFIGURATION_INVALID", "Cấu hình kích hoạt tài khoản kiểm duyệt viên không hợp lệ.");
        public static readonly Error EmailRequired = new("AUTH_EMAIL_REQUIRED", "Vui lòng nhập email.");

        public static readonly Error InvalidEmail = new("AUTH_EMAIL_INVALID", "Định dạng email không hợp lệ.");

        public static readonly Error FullNameRequired = new("AUTH_FULLNAME_REQUIRED", "Vui lòng nhập họ tên.");

        public static readonly Error InvalidFullName = new("AUTH_FULLNAME_INVALID", "Họ tên chỉ được chứa chữ cái và khoảng trắng.");

        public static readonly Error UsernameRequired = new("AUTH_USERNAME_REQUIRED", "Vui lòng nhập tên đăng nhập.");

        public static readonly Error InvalidUsername = new("AUTH_USERNAME_INVALID", "Tên đăng nhập chỉ được chứa chữ cái, chữ số và dấu gạch dưới.");

        public static readonly Error PasswordRequired = new("AUTH_PASSWORD_REQUIRED", "Vui lòng nhập mật khẩu.");

        public static readonly Error InvalidPasswordLength = new("AUTH_PASSWORD_LENGTH_INVALID", "Mật khẩu phải có từ 6 đến 20 ký tự.");

        public static readonly Error PhoneNumberRequired = new("AUTH_PHONE_REQUIRED", "Vui lòng nhập số điện thoại.");

        public static readonly Error InvalidPhoneNumber = new("AUTH_PHONE_INVALID", "Số điện thoại phải có 9 hoặc 10 chữ số và bắt đầu bằng số 0.");

        // AUTHENTICATION

        public static readonly Error InvalidCredential = new("AUTH_INVALID_CREDENTIAL", "Tên đăng nhập hoặc mật khẩu không đúng.");

        public static readonly Error UserNotFound = new("AUTH_USER_NOT_FOUND", "Không tìm thấy người dùng.");

        public static readonly Error InvalidRefreshToken = new("AUTH_INVALID_REFRESH_TOKEN", "Mã làm mới phiên đăng nhập không hợp lệ.");

        public static readonly Error ExpiredRefreshToken = new("AUTH_REFRESH_TOKEN_EXPIRED", "Mã làm mới phiên đăng nhập đã hết hạn.");

        public static readonly Error RevokedRefreshToken = new("AUTH_REFRESH_TOKEN_REVOKED", "Mã làm mới phiên đăng nhập đã bị thu hồi.");

        public static readonly Error EmailNotVerified = new("AUTH_EMAIL_NOT_VERIFIED", "Email chưa được xác minh.");

        public static readonly Error InvalidOtp = new("AUTH_INVALID_OTP", "Mã OTP không hợp lệ.");

        public static readonly Error OtpSendFailed = new("AUTH_OTP_SEND_FAILED", "Không thể gửi mã OTP. Vui lòng thử lại sau.");

        public static Error OtpRateLimited(int minutes) => new("AUTH_OTP_RATE_LIMITED", $"Bạn chỉ có thể yêu cầu OTP lại sau {minutes} phút.");

        // ACCOUNT STATUS

        public static readonly Error AccountSuspended = new("AUTH_ACCOUNT_SUSPENDED", "Tài khoản đã bị đình chỉ.");

        public static readonly Error CannotLockSelf = new("AUTH_CANNOT_LOCK_SELF", "Quản trị viên không thể khóa tài khoản của chính mình.");

        public static readonly Error CannotLockAdmin = new("AUTH_CANNOT_LOCK_ADMIN", "Không thể khóa tài khoản của quản trị viên khác.");

        public static readonly Error AlreadyLocked = new("AUTH_ACCOUNT_ALREADY_LOCKED", "Tài khoản đã bị khóa.");

        public static readonly Error NotLocked = new("AUTH_ACCOUNT_NOT_LOCKED", "Tài khoản chưa bị khóa.");

        public static readonly Error AccountDeleted = new("AUTH_ACCOUNT_DELETED", "Tài khoản đã bị xóa.");

        // REGISTRATION

        public static readonly Error EmailExists = new("AUTH_EMAIL_EXISTS", "Email đã tồn tại.");

        public static readonly Error UsernameExists = new("AUTH_USERNAME_EXISTS", "Tên đăng nhập đã tồn tại.");

        public static readonly Error InvalidRegistrationSession = new("AUTH_INVALID_REGISTRATION_SESSION", "Phiên đăng ký không hợp lệ hoặc đã hết hạn.");
    }

    public static class ProfileErrors
    {
        public static readonly Error UserNotFound = new("AUTH_USER_NOT_FOUND", "Không tìm thấy người dùng. Vui lòng thử lại.");
        public static readonly Error ProfileNotFound = new("AUTH_PROFILE_NOT_FOUND", "Không tìm thấy hồ sơ. Vui lòng thử lại.");
        public static readonly Error AvatarUploadFailed = new("PROFILE_AVATAR_UPLOAD_FAILED", "Không thể tải ảnh đại diện hoặc logo lên hệ thống lưu trữ.");
        public static readonly Error AvatarUpdateFailed = new("PROFILE_AVATAR_UPDATE_FAILED", "Không thể cập nhật ảnh đại diện hoặc logo.");
    }

    public static class CategoryErrors
    {
        public static readonly Error CategoryNotFound = new("CATEGORY_NOT_FOUND", "Không tìm thấy danh mục.");

        public static readonly Error CategoryAlreadyExists = new("CATEGORY_ALREADY_EXISTS", "Tên danh mục đã tồn tại.");

        public static readonly Error CategoryInactive = new("CATEGORY_INACTIVE", "Danh mục đã bị vô hiệu hóa.");
    }

    public static class BrandErrors
    {
        public static readonly Error BrandNotFound = new("BRAND_NOT_FOUND", "Thương hiệu không tồn tại.");

        public static readonly Error BrandAlreadyExists = new("BRAND_ALREADY_EXISTS", "Thương hiệu đã tồn tại.");
    }

    public static class ProductTypeErrors
    {
        public static readonly Error ProductTypeNotFound = new("PRODUCT_TYPE_NOT_FOUND", "Loại sản phẩm không tồn tại.");

        public static readonly Error ProductTypeAlreadyExists = new("PRODUCT_TYPE_ALREADY_EXISTS", "Loại sản phẩm đã tồn tại.");

        public static readonly Error CategoryNotFound = new("CATEGORY_NOT_FOUND", "Danh mục không tồn tại.");

        public static readonly Error AttributeAlreadyExists = new("ATTRIBUTE_ALREADY_EXISTS", "Thuộc tính đã tồn tại.");
        public static readonly Error AttributeNotFound = new("ATTRIBUTE_NOT_FOUND", "Thuộc tính không tồn tại.");
        public static readonly Error AttributeInUse = new("ATTRIBUTE_ALREADY_IN_USE", "Thuộc tính đang được sử dụng.");
        public static readonly Error CannotChangeDataTypeInUse = new("DATA_TYPE_CANNOT_CHANGE_IN_USE", "Không thể thay đổi kiểu dữ liệu vì thuộc tính đang được sử dụng.");
        public static readonly Error CannotChangeInputModeInUse = new("INPUT_MODE_CANNOT_CHANGE_IN_USE", "Không thể thay đổi chế độ nhập vì thuộc tính đang được sử dụng.");

    }

    public static class ProductAttributeErrors
    {
        public static readonly Error AttributeNotFound = new("ATTRIBUTE_NOT_FOUND", "Thuộc tính sản phẩm không tồn tại.");

        public static readonly Error AttributeAlreadyExists = new("ATTRIBUTE_ALREADY_EXISTS", "Thuộc tính đã tồn tại trong loại sản phẩm này.");

        public static readonly Error RequiredAttributeMissing = new("ATTRIBUTE_REQUIRED_MISSING", "Thiếu thuộc tính bắt buộc.");

    }

    public static class ProductAttributeOptionErrors
    {
        public static readonly Error OptionNotFound = new("ATTRIBUTE_OPTION_NOT_FOUND", "Tùy chọn thuộc tính không tồn tại.");

        public static readonly Error OptionAlreadyExists = new("ATTRIBUTE_OPTION_ALREADY_EXISTS", "Tùy chọn thuộc tính đã tồn tại.");
        public static readonly Error OptionInUse = new("ATTRIBUTE_OPTION_ALREADY_IN_USE", "Tùy chọn thuộc tính đang được sử dụng.");
    }

    public static class ProductErrors
    {
        public static readonly Error ProductNotFound = new("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");

        public static readonly Error InvalidCategory = new("PRODUCT_INVALID_CATEGORY", "Danh mục sản phẩm không hợp lệ.");

        public static readonly Error InvalidProductType = new("PRODUCT_INVALID_PRODUCT_TYPE", "Loại sản phẩm không phù hợp với danh mục.");
        public static readonly Error InvalidBrand = new("PRODUCT_INVALID_BRAND", "Thương hiệu không hợp lệ.");
    }

    public static class PostErrors
    {
        public static readonly Error NotFound = new("POST_NOT_FOUND", "Không tìm thấy bài đăng.");

        public static readonly Error InvalidPostType = new("POST_INVALID_TYPE", "Loại bài đăng không hợp lệ.");

        public static readonly Error UnauthorizedOwner = new("POST_UNAUTHORIZED", "Bạn không có quyền thực hiện thao tác này trên bài đăng.");

        public static readonly Error PostAlreadyClosedOrDeleted = new("POST_ALREADY_CLOSED_OR_DELETED", "Bài đăng đã đóng hoặc đã bị xóa.");

        public static readonly Error PostAlreadySuspended = new("POST_ALREADY_SUSPENDED", "Bài đăng đã bị đình chỉ.");

        public static readonly Error Forbidden = new("POST_FORBIDDEN", "Bạn không có quyền truy cập bài đăng này.");

        public static readonly Error PostExpired = new("POST_EXPIRED", "Bài đăng đã hết thời hạn cho phép chỉnh sửa.");

        public static readonly Error RoleNotAllowed = new("POST_ROLE_NOT_ALLOWED", "Vai trò tài khoản của bạn không được phép tạo loại bài đăng này.");

        public static Error InvalidUpdateQuantity(int soldQuantity, int requestedQuantity)
            => new(
                "POST_INVALID_QUANTITY",
                $"Số lượng cập nhật ({requestedQuantity}) không thể nhỏ hơn số lượng đã bán/giao dịch ({soldQuantity}).");
    }

    public static class OfferErrors
    {
        public static readonly Error Expired = new Error("Offer.Expired", "Đề nghị đã hết hạn sau 3 phút chờ phản hồi. Bạn có thể gửi đề nghị mới nếu bài đăng còn khả dụng.");
        public static readonly Error NotFound = new("OFFER_NOT_FOUND", "Không tìm thấy đề nghị.");

        public static readonly Error Forbidden = new("OFFER_FORBIDDEN", "Bạn không có quyền thực hiện thao tác với đề nghị này.");

        public static readonly Error NotPending = new("OFFER_NOT_PENDING", "Đề nghị không còn ở trạng thái chờ phản hồi.");

        public static readonly Error PostNotFound = new("OFFER_POST_NOT_FOUND", "Không tìm thấy bài bán được chọn.");

        public static readonly Error BuyPostNotFound = new("OFFER_BUY_POST_NOT_FOUND", "Không tìm thấy bài mua cần gửi chào bán.");

        public static readonly Error InvalidSellPostType = new("OFFER_INVALID_SELL_POST_TYPE", "Bài đăng được chọn không phải là bài bán.");

        public static readonly Error InvalidBuyPostType = new("OFFER_INVALID_BUY_POST_TYPE", "Bài đăng nhận chào bán không phải là bài mua.");

        public static readonly Error PostNotActive = new("OFFER_POST_NOT_ACTIVE", "Bài đăng hiện không hoạt động.");

        public static readonly Error SellPostNotActive = new("OFFER_SELL_POST_NOT_ACTIVE", "Bài bán đã đóng hoặc không còn hoạt động nên không thể gửi chào.");

        public static readonly Error SellPostExpired = new("OFFER_SELL_POST_EXPIRED", "Bài bán đã hết hạn nên không thể gửi chào.");

        public static readonly Error SellPostOutOfStock = new("OFFER_SELL_POST_OUT_OF_STOCK", "Bài bán không còn sản phẩm khả dụng để gửi chào.");

        public static readonly Error BuyPostNotActive = new("OFFER_BUY_POST_NOT_ACTIVE", "Bài mua hiện đã đóng. Chủ bài cần mở lại hoặc tăng số lượng trước khi nhận chào bán mới.");

        public static readonly Error BuyPostExpired = new("OFFER_BUY_POST_EXPIRED", "Bài mua đã hết thời hạn nhận chào bán.");

        public static readonly Error BuyPostFulfilled = new("OFFER_BUY_POST_FULFILLED", "Bài mua đã đạt đủ số lượng cần thu mua.");

        public static readonly Error BuyerNotActive = new("OFFER_BUYER_NOT_ACTIVE", "Tài khoản chủ bài mua hiện không hoạt động nên chưa thể nhận chào bán.");

        public static readonly Error InvalidQuantity = new("OFFER_INVALID_QUANTITY", "Số lượng đề nghị phải lớn hơn 0.");

        public static readonly Error CannotOfferOwnPost = new("OFFER_CANNOT_OFFER_OWN_POST", "Bạn không thể gửi đề nghị cho bài đăng của chính mình.");

        public static readonly Error DuplicatePending = new("OFFER_DUPLICATE_PENDING", "Bài bán này đang có đề nghị chờ phản hồi giữa bạn và người mua. Vui lòng xử lý hoặc chờ đề nghị hết hạn sau 3 phút trước khi gửi lại.");

        public static readonly Error UnfinishedNegotiation = new("OFFER_UNFINISHED_NEGOTIATION", "Bài bán này đang có phiên thương lượng hoặc thỏa thuận chưa hoàn tất với người mua. Vui lòng hoàn tất, hủy hoặc chờ phiên hiện tại hết hạn trước khi gửi lại.");

        public static readonly Error RoleNotAllowed = new("OFFER_ROLE_NOT_ALLOWED", "Loại tài khoản của bạn không được phép thực hiện thao tác chào bán này.");

        public static readonly Error BusinessCannotOfferBuyPost = new("OFFER_B2B_NOT_ALLOWED", "Tài khoản doanh nghiệp không thể gửi chào bán cho bài mua của doanh nghiệp.");
        public static readonly Error UserNotActive = new("OFFER_USER_NOT_ACTIVE", "Tài khoản hiện không hoạt động nên không thể gửi hoặc nhận đề nghị.");

        public static Error PriceOutOfRange(decimal minPrice, decimal maxPrice)
            => new("OFFER_PRICE_OUT_OF_RANGE",
                   $"Giá đề nghị phải nằm trong khoảng từ {minPrice:N0}đ đến {maxPrice:N0}đ.");

        public static Error SellerRequestPriceOutOfRange(decimal offerPrice, decimal? minPrice, decimal? maxPrice)
        {
            var allowedRange = minPrice.HasValue && maxPrice.HasValue
                ? $"từ {minPrice.Value:N0}đ đến {maxPrice.Value:N0}đ"
                : minPrice.HasValue
                    ? $"từ {minPrice.Value:N0}đ trở lên"
                    : $"không vượt quá {maxPrice!.Value:N0}đ";
            return new Error("OFFER_PRICE_OUT_OF_RANGE",
                $"Giá chào bán {offerPrice:N0}đ nằm ngoài khoảng {allowedRange} đang hiển thị trên bài mua. Vui lòng điều chỉnh giá rồi gửi lại.");
        }

        public static Error QuantityExceedsRemaining(int requested, int remaining)
            => new("OFFER_QUANTITY_EXCEEDS_REMAINING",
                   $"Số lượng đề nghị ({requested}) vượt quá số lượng còn khả dụng ({remaining}).");

        public static Error SellQuantityExceedsRemaining(int requested, int remaining)
            => new("OFFER_SELL_QUANTITY_EXCEEDS_REMAINING",
                $"Số lượng chào bán ({requested}) vượt quá số lượng sản phẩm còn khả dụng ({remaining}) của bài bán.");

        public static Error BuyQuantityExceedsRemaining(int requested, int remaining)
            => new("OFFER_BUY_QUANTITY_EXCEEDS_REMAINING",
                $"Số lượng chào bán ({requested}) vượt quá số lượng bài mua còn cần thu mua ({remaining}).");

        public static Error OfferTermsChanged(decimal currentPrice, int currentQuantity)
            => new("OFFER_TERMS_CHANGED",
                   $"Đối phương vừa cập nhật đề nghị: giá {currentPrice:N0}đ, số lượng {currentQuantity}. " +
                   "Vui lòng xem lại trước khi xác nhận.");
    }

    public static class NegotiationErrors
    {
        public static readonly Error AgreementBlocksCancellation = new("Negotiation.AgreementBlocksCancellation", "Thỏa thuận đang chờ xác nhận hoặc thanh toán và sẽ tự hết hạn khi hết thời gian. Bạn không thể hủy phiên thương lượng tại bước này.");
        public static readonly Error CancellationResolutionPending = new("Negotiation.PaymentResolutionPending", "Thỏa thuận đã quá hạn. Hệ thống đang xử lý hết hạn hoặc đối soát thanh toán. Bạn không thể hủy phiên thương lượng hoặc thanh toán thêm vào lúc này.");
        public static readonly Error OrderBlocksCancellation = new("Negotiation.OrderBlocksCancellation", "Giao dịch đã chuyển sang bước đơn hàng. Bạn không thể hủy phiên thương lượng; vui lòng xem đơn hàng và sử dụng chức năng tranh chấp khi cần.");
        public static readonly Error AgreementClosed = new("Negotiation.AgreementClosed", "Thỏa thuận đã kết thúc. Bạn không thể hủy phiên thương lượng này.");
        public static readonly Error Expired = new Error("Negotiation.Expired", "Phiên thương lượng đã hết hạn sau 5 phút không có hoạt động. Bạn có thể gửi yêu cầu mới nếu bài đăng còn khả dụng.");
        public static readonly Error NotFound = new("NEGOTIATION_NOT_FOUND", "Không tìm thấy phiên thương lượng.");

        public static readonly Error NotOpen = new("NEGOTIATION_NOT_OPEN", "Phiên thương lượng không còn ở trạng thái đang mở.");

        public static readonly Error Forbidden = new("NEGOTIATION_FORBIDDEN", "Bạn không có quyền truy cập hoặc thực hiện thao tác này trong phiên thương lượng.");

        public static readonly Error InvalidStatusForCounter = new("NEGOTIATION_INVALID_STATUS_FOR_COUNTER", "Chỉ có thể gửi đề nghị đối ứng khi phiên thương lượng đang mở.");
        public static readonly Error AlreadyExists = new("NEGOTIATION_ALREADY_EXISTS", "Đề nghị này đã có phiên thương lượng.");
        public static readonly Error ProposalNotFound = new("NEGOTIATION_PROPOSAL_NOT_FOUND", "Không tìm thấy đề nghị trong phiên thương lượng.");
        public static readonly Error InvalidStatusForCancel = new("NEGOTIATION_INVALID_STATUS_FOR_CANCEL", "Chỉ có thể hủy phiên thương lượng đang mở hoặc đã thống nhất điều khoản.");
    }

    public static class MessageErrors
    {
        public static readonly Error NotFound = new("MESSAGE_NOT_FOUND", "Không tìm thấy tin nhắn.");
        public static readonly Error Forbidden = new("MESSAGE_FORBIDDEN", "Bạn không có quyền truy cập tin nhắn này.");
        public static readonly Error ClientMessageIdConflict = new("MESSAGE_CLIENT_ID_CONFLICT", "Đã tồn tại tin nhắn có cùng mã tin nhắn phía ứng dụng khách.");
        public static readonly Error InvalidMessage = new("MESSAGE_INVALID", "Tin nhắn không hợp lệ hoặc không thể xử lý.");
        public static readonly Error NegotiationReadOnly = new("MESSAGE_NEGOTIATION_READ_ONLY", "Không thể gửi tin nhắn trong phiên thương lượng không ở trạng thái đang mở.");
    }

    public static class CartErrors
    {
        public static readonly Error ItemNotFound = new("CART_ITEM_NOT_FOUND", "Không tìm thấy mục trong giỏ hàng.");

        public static readonly Error ItemExists = new("CART_ITEM_EXISTS", "Bài đăng đã có trong giỏ hàng của bạn.");

        public static readonly Error PostNotFound = new("CART_POST_NOT_FOUND", "Bài đăng không tồn tại.");

        public static readonly Error PostNotActive = new("CART_POST_NOT_ACTIVE", "Bài đăng hiện không hoạt động.");

        public static readonly Error CannotAddOwnPost = new("CART_CANNOT_ADD_OWN_POST", "Bạn không thể thêm bài đăng của chính mình vào giỏ hàng.");

        public static readonly Error InvalidQuantity = new("CART_INVALID_QUANTITY", "Số lượng phải lớn hơn 0.");

        public static Error QuantityExceedsRemaining(int requested, int remaining)
            => new("CART_QUANTITY_EXCEEDS_REMAINING",
                   $"Số lượng ({requested}) vượt quá số lượng còn lại ({remaining}).");

        public static readonly Error Forbidden = new("CART_FORBIDDEN", "Bạn không có quyền truy cập mục này trong giỏ hàng.");
    }

    public static class ContentDisputeErrors
    {
        public static readonly Error DuplicateOpenReport = new("DISPUTE_DUPLICATE_OPEN_REPORT",
            "Bạn đã có báo cáo đang chờ xử lý hoặc đang được xem xét cho nội dung này.");
        public static readonly Error SelfReportNotAllowed = new("DISPUTE_SELF_REPORT_NOT_ALLOWED",
            "Không thể báo cáo nội dung do chính bạn tạo.");
        public static readonly Error InvalidCategory = new("DISPUTE_INVALID_CONTENT_CATEGORY",
            "Lý do báo cáo không phù hợp với loại nội dung.");
        public static readonly Error TargetUnavailable = new("DISPUTE_CONTENT_UNAVAILABLE",
            "Nội dung đã bị xóa, ẩn hoặc đình chỉ và không thể báo cáo.");
        public static readonly Error ReviewNotFound = new("Review.NotFound", "Không tìm thấy đánh giá.");
    }

    public static class DisputeErrors
    {
        public static readonly Error NotFound =
            new("DISPUTE_NOT_FOUND", "Không tìm thấy tranh chấp.");

        public static readonly Error Forbidden =
            new("DISPUTE_FORBIDDEN", "Bạn không có quyền thực hiện thao tác này đối với tranh chấp này.");

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

        public static Error InvalidCategory(string code) =>
            new("DISPUTE_INVALID_CATEGORY", $"Loại tranh chấp '{code}' không phù hợp với ngữ cảnh đơn hàng hiện tại.");

        public static readonly Error CloseNotAllowed =
            new("DISPUTE_CLOSE_NOT_ALLOWED", "Chỉ có thể đóng tranh chấp đang ở trạng thái chờ xử lý.");

        public static readonly Error AlreadyUnderReview =
            new("DISPUTE_ALREADY_UNDER_REVIEW", "Tranh chấp đã được kiểm duyệt viên tiếp nhận và không thể tự đóng.");

        public static readonly Error ClaimNotAllowed =
            new("DISPUTE_CLAIM_NOT_ALLOWED", "Chỉ có thể tiếp nhận tranh chấp đang ở trạng thái chờ xử lý.");

        public static readonly Error AlreadyClaimed =
            new("DISPUTE_ALREADY_CLAIMED", "Tranh chấp đã được kiểm duyệt viên khác tiếp nhận.");

        public static readonly Error DecisionNotAllowed =
            new("DISPUTE_DECISION_NOT_ALLOWED", "Chỉ có thể đưa ra kết luận khi tranh chấp đang được kiểm duyệt viên xử lý.");

        public static readonly Error NotAssignedModerator =
            new("DISPUTE_NOT_ASSIGNED_MODERATOR", "Bạn không phải kiểm duyệt viên đang phụ trách tranh chấp này.");

        public static readonly Error ReturnVerificationNotAllowed =
            new("DISPUTE_RETURN_VERIFICATION_NOT_ALLOWED", "Tranh chấp hiện không ở trạng thái chờ xác minh hoàn trả.");

        public static Error ReturnVerificationNotDue(DateTime dueAt) =>
            new("DISPUTE_RETURN_VERIFICATION_NOT_DUE", $"Kiểm duyệt viên chỉ có thể xác minh hoàn trả sau thời hạn {dueAt:O}.");
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
            new("Order.DirectHandoverOnly", "Xác nhận bàn giao của người bán chỉ áp dụng cho giao nhận trực tiếp.");

        public static readonly Error ShipmentNotFound =
            new("Order.ShipmentNotFound", "Không tìm thấy thông tin vận chuyển của đơn hàng.");

        public static readonly Error ShipmentNotDelivered =
            new("Order.ShipmentNotDelivered", "Đơn vận chuyển chưa được xác nhận giao thành công.");

        public static readonly Error SellerReadyRequired =
            new("Order.SellerReadyRequired", "Người bán phải xác nhận đã chuẩn bị hàng trước khi thực hiện xác nhận giao nhận.");

        public static readonly Error CancellationNotAllowed =
            new("Order.CancellationNotAllowed", "Đơn hàng đã vượt mốc cho phép hủy trực tiếp. Nếu phát sinh vấn đề, vui lòng sử dụng tranh chấp.");

        public static readonly Error ActiveDisputeBlocksCancellation =
            new("Order.ActiveDisputeBlocksCancellation", "Đơn hàng đang có tranh chấp nên không thể hủy trực tiếp.");

        public static readonly Error InvalidCompletionState =
            new("Order.InvalidCompletionState", "Đơn hàng đã hoàn tất nhưng không xác định được thời điểm hoàn tất hoặc giao hàng.");

        public static readonly Error NotDisputing =
            new(
                "Order.NotDisputing",
                "Đơn hàng hiện không ở trạng thái tranh chấp.");

        public static readonly Error ReturnConfirmationNotAllowed =
            new("Order.ReturnConfirmationNotAllowed", "Đơn hàng hiện không ở trạng thái chờ hoàn trả.");

        public static Error ReturnDeadlineExpired(DateTime dueAt) =>
            new("Order.ReturnDeadlineExpired", $"Thời hạn xác nhận trả hàng đã kết thúc lúc {dueAt:O}.");
    }

    public static class PlatformPolicyErrors
    {
        public static Error ActiveNotFound(PlatformPolicyType policyType) =>
            new("PlatformPolicy.ActiveNotFound", $"Không tìm thấy chính sách đang hoạt động cho '{policyType}'.");

        public static Error InvalidContent(PlatformPolicyType policyType) =>
            new("PlatformPolicy.InvalidContent", $"Nội dung cấu hình của chính sách '{policyType}' không hợp lệ.");

        public static readonly Error InvalidDisputePolicy =
            new("PlatformPolicy.InvalidDisputePolicy", "Thời gian tranh chấp của tài khoản uy tín thấp không được ngắn hơn thời gian tranh chấp thông thường.");

        public static readonly Error InvalidAppointmentPolicy =
            new("PlatformPolicy.InvalidAppointmentPolicy", "Thời hạn yêu cầu đổi lịch phải lớn hơn hoặc bằng thời hạn được phép hủy lịch.");

        public static readonly Error InvalidPaymentPolicy =
            new(
        "PlatformPolicy.InvalidPaymentPolicy",
        "Cấu hình chính sách thanh toán không hợp lệ.");

        public static readonly Error InvalidOrderPolicy =
            new(
                "PlatformPolicy.InvalidOrderPolicy",
                "Cấu hình chính sách đơn hàng không hợp lệ.");
        public static Error VersionNotFound(PlatformPolicyType policyType, int version) =>
            new("PlatformPolicy.VersionNotFound", $"Không tìm thấy phiên bản {version} của chính sách '{policyType}'.");

        public static readonly Error VersionAlreadyActive =
            new("PlatformPolicy.VersionAlreadyActive", "Phiên bản này hiện đang là phiên bản được áp dụng.");

        public static Error UnsupportedType(string policyType) =>
            new("PlatformPolicy.UnsupportedType", $"Loại chính sách '{policyType}' không được hệ thống hỗ trợ.");

        public static readonly Error InvalidWithdrawalPolicy =
            new("PlatformPolicy.InvalidWithdrawalPolicy", "Chính sách rút tiền không hợp lệ. Các hạn mức tiền phải là số nguyên dương; số tiền rút tối thiểu không được vượt quá số tiền rút tối đa, số tiền rút tối đa không được vượt quá hạn mức rút trong ngày và số lần rút tối đa trong ngày phải lớn hơn 0.");
    }

    public static class AgreementErrors
    {
        public static readonly Error Expired = new Error("Agreement.Expired", "Thỏa thuận đã hết hạn 15 phút. Bạn có thể gửi yêu cầu mới nếu bài đăng còn khả dụng.");
        public static readonly Error PaymentResolutionPending = new Error("Agreement.PaymentResolutionPending", "Đã hết hạn xác nhận và thanh toán. Hệ thống đang đối soát giao dịch; phần giữ chỗ chưa được giải phóng. Vui lòng không thanh toán thêm.");
        public static readonly Error DeadlinePassed = new Error("Agreement.DeadlinePassed", "Đã hết 15 phút xác nhận và thanh toán. Không thể thanh toán thêm; vui lòng chờ hệ thống đối soát và xử lý hết hạn.");
        public static readonly Error NotFound =
            new("Agreement.NotFound", "Không tìm thấy thỏa thuận.");

        public static readonly Error AlreadyExists =
            new("Agreement.AlreadyExists", "Thỏa thuận đã tồn tại.");

        public static readonly Error Forbidden =
            new("Agreement.Forbidden", "Bạn không có quyền thực hiện thao tác này trên thỏa thuận.");

        public static readonly Error InvalidStatus =
            new("Agreement.InvalidStatus", "Trạng thái hiện tại của thỏa thuận không cho phép thực hiện thao tác này.");

        public static readonly Error CancelNotAvailable =
            new("Agreement.CancelNotAvailable", "Chỉ có thể hủy hợp đồng sau khi cả hai bên đã xác nhận và trước khi hết hạn.");

        public static readonly Error EditNotAllowedAfterConfirmation =
            new("Agreement.EditNotAllowedAfterConfirmation", "Hợp đồng đã được cả hai bên xác nhận và không thể chỉnh sửa. Hai bên có thể thanh toán hoặc hủy hợp đồng trước khi thanh toán.");

        public static readonly Error PaymentInProgress =
            new("Agreement.PaymentInProgress", "Thanh toán đang được xử lý hoặc chưa xác minh được. Vui lòng kiểm tra lại giao dịch rồi thử hủy sau.");

        public static readonly Error AlreadyPaid =
            new("Agreement.AlreadyPaid", "Hợp đồng đã được thanh toán hoặc đã phát sinh đơn hàng. Vui lòng xử lý tại đơn hàng.");

        public static readonly Error AlreadyConfirmed =
            new("Agreement.AlreadyConfirmed", "Bạn đã xác nhận thỏa thuận này rồi.");

        public static readonly Error RevisionMismatch =
            new("Agreement.RevisionMismatch", "Nội dung thỏa thuận vừa được cập nhật. Vui lòng tải lại và xem nội dung mới nhất trước khi xác nhận.");

        public static readonly Error OnlySellerCanCreate =
            new("Agreement.OnlySellerCanCreate", "Chỉ người bán mới có quyền tạo thỏa thuận.");

        public static readonly Error AppointmentScheduleMissing =
            new("Agreement.AppointmentScheduleMissing", "Thỏa thuận chưa có thời gian lịch hẹn hợp lệ.");

        public static readonly Error AppointmentScheduleExpired =
            new("Agreement.AppointmentScheduleExpired", "Thời gian lịch hẹn của thỏa thuận đã qua. Vui lòng cập nhật lại thỏa thuận.");
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
            new("Appointment.CheckInInspectionOnly", "Xác nhận có mặt chỉ áp dụng cho lịch kiểm định.");

        public static readonly Error CheckInAlreadyStarted =
            new("Appointment.CheckInAlreadyStarted", "Không thể thay đổi lịch sau khi một trong hai bên đã xác nhận có mặt.");

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
            new("Appointment.CheckInNotOpen", $"Xác nhận có mặt chưa mở. Có thể xác nhận có mặt từ {openAt:O}.");

        public static Error RescheduleCutoffPassed(DateTime cutoff) =>
            new("Appointment.RescheduleCutoffPassed", $"Đã quá thời hạn yêu cầu đổi lịch. Hạn cuối: {cutoff:O}.");

        public static Error CancellationCutoffPassed(DateTime cutoff) =>
            new("Appointment.CancellationCutoffPassed", $"Đã quá thời hạn hủy lịch. Hạn cuối: {cutoff:O}.");

        public static Error CollectionConfirmationNotOpen(DateTime scheduledAt) =>
            new("Appointment.CollectionConfirmationNotOpen", $"Chưa đến ngày được phép xác nhận giao nhận. Lịch thu gom: {scheduledAt:O}.");

        public static readonly Error ScheduleOutsideBusinessHours =
            new("Appointment.ScheduleOutsideBusinessHours", "Thời gian lịch hẹn phải nằm trong khoảng 08:00 đến 22:00 theo giờ Việt Nam.");
    }

    public static class InspectionErrors
    {
        public static readonly Error NotFound =
            new("Inspection.NotFound", "Không tìm thấy biểu mẫu kiểm định.");

        public static readonly Error AlreadyExists =
            new("Inspection.AlreadyExists", "Lịch kiểm định này đã có biểu mẫu kiểm định.");

        public static readonly Error BuyerOnly =
            new("Inspection.BuyerOnly", "Chỉ người mua thực hiện kiểm định mới có quyền thao tác biểu mẫu.");

        public static readonly Error SellerOnly =
            new("Inspection.SellerOnly", "Chỉ người bán của giao dịch mới có quyền xác nhận kết quả kiểm định.");

        public static readonly Error InvalidAppointment =
            new("Inspection.InvalidAppointment", "Lịch hẹn này không phải lịch kiểm định hợp lệ.");

        public static readonly Error AppointmentNotInProgress =
            new("Inspection.AppointmentNotInProgress", "Biểu mẫu chỉ được xử lý khi lịch kiểm định đang diễn ra.");

        public static readonly Error BothCheckInRequired =
            new("Inspection.BothCheckInRequired", "Người mua và người bán phải xác nhận có mặt trước khi tiến hành kiểm định.");

        public static readonly Error DraftOnly =
            new("Inspection.DraftOnly", "Chỉ biểu mẫu ở trạng thái bản nháp mới được chỉnh sửa hoặc gửi.");

        public static readonly Error PendingConfirmationOnly =
            new("Inspection.PendingConfirmationOnly", "Biểu mẫu không ở trạng thái chờ người bán xác nhận.");

        public static readonly Error RevisionMismatch =
            new("Inspection.RevisionMismatch", "Biểu mẫu kiểm định đã được cập nhật. Vui lòng tải lại phiên bản mới nhất.");

        public static readonly Error Incomplete =
            new("Inspection.Incomplete", "Vui lòng hoàn thành đầy đủ danh sách kiểm tra và kết luận kiểm định trước khi gửi.");

        public static readonly Error SuggestedPriceRequired =
            new("Inspection.SuggestedPriceRequired", "Kết luận điều chỉnh giá yêu cầu nhập giá mới.");

        public static readonly Error SuggestedPriceUnchanged =
            new("Inspection.SuggestedPriceUnchanged", "Giá đề xuất mới phải khác giá giao dịch hiện tại.");

        public static readonly Error InvalidOrderPrice =
            new("Inspection.InvalidOrderPrice", "Không xác định được giá giao dịch hiện tại.");

        public static readonly Error AcceptedRequired =
            new("Inspection.AcceptedRequired", "Biểu mẫu phải được người bán xác nhận trước khi tiếp tục thu gom.");

        public static readonly Error FailedCannotCollect =
            new("Inspection.FailedCannotCollect", "Kết quả kiểm định không đạt nên không thể tiếp tục thu gom.");

        public static readonly Error CollectActionAlreadySelected =
            new("Inspection.CollectActionAlreadySelected", "Phương án thu gom đã được lựa chọn.");

        public static readonly Error DepositMissing =
            new("Inspection.DepositMissing", "Không xác định được khoản tiền cọc cần hoàn.");

        public static readonly Error CollectionAddressRequired =
            new("Inspection.CollectionAddressRequired", "Cần đầy đủ địa chỉ lấy và giao hàng.");

        public static readonly Error CollectionScheduleFailed =
            new("Inspection.CollectionScheduleFailed", "Không thể tạo lịch thu gom.");
    }

    public static class PaymentErrors
    {
        public static readonly Error NotFound =
            new("Payment.NotFound", "Không tìm thấy giao dịch thanh toán.");

        public static readonly Error RefundPaymentNotFound =
            new("Payment.RefundPaymentNotFound", "Không tìm thấy giao dịch thanh toán để thực hiện hoàn tiền.");

        public static readonly Error RefundWalletNotFound =
            new("Payment.RefundWalletNotFound", "Không tìm thấy ví cần thiết để thực hiện hoàn tiền.");

        public static readonly Error InsufficientHeldBalance =
            new("Payment.InsufficientHeldBalance", "Số dư tiền tạm giữ của đơn hàng không đủ để thực hiện hoàn tiền.");

        public static readonly Error InvalidRefundAmount =
            new("Payment.InvalidRefundAmount", "Số tiền hoàn không hợp lệ.");

        public static readonly Error AlreadyRefunded =
            new("Payment.AlreadyRefunded", "Khoản thanh toán đã được hoàn toàn bộ.");

        public static readonly Error OrderHeldAmountNotFound =
            new("Payment.OrderHeldAmountNotFound", "Đơn hàng không còn khoản tiền tạm giữ có thể hoàn.");

        public static readonly Error InvalidReleaseAmount =
            new("Payment.InvalidReleaseAmount", "Số tiền tạm giữ của đơn hàng không hợp lệ để giải ngân.");

        public static readonly Error ReleaseOrderNotCompleted =
            new("Payment.ReleaseOrderNotCompleted", "Chỉ có thể giải ngân đơn hàng đã hoàn tất.");

        public static readonly Error ReleaseWindowMissing =
            new("Payment.ReleaseWindowMissing", "Đơn hàng chưa xác định thời điểm kết thúc thời hạn tranh chấp.");

        public static Error ReleaseWindowNotEnded(DateTime disputeWindowEndsAt) =>
            new("Payment.ReleaseWindowNotEnded", $"Chưa thể giải ngân trước khi thời hạn tranh chấp kết thúc lúc {disputeWindowEndsAt:O}.");

        public static readonly Error ActiveDisputeBlocksRelease =
            new("Payment.ActiveDisputeBlocksRelease", "Đơn hàng đang có tranh chấp nên chưa thể giải ngân.");

        public static readonly Error ReleasePaymentNotFound =
            new("Payment.ReleasePaymentNotFound", "Không tìm thấy giao dịch thanh toán hợp lệ để giải ngân.");

        public static readonly Error ReleaseWalletNotFound =
            new("Payment.ReleaseWalletNotFound", "Không tìm thấy ví cần thiết để giải ngân.");

        public static readonly Error ReleaseOrderHeldAmountNotFound =
            new("Payment.ReleaseOrderHeldAmountNotFound", "Đơn hàng không còn khoản tiền tạm giữ có thể giải ngân.");

        public static readonly Error InsufficientHeldBalanceForRelease =
            new("Payment.InsufficientHeldBalanceForRelease", "Số dư tiền tạm giữ của đơn hàng không đủ để giải ngân cho đơn hàng.");

        public static readonly Error ReleaseFailed =
            new("Payment.ReleaseFailed", "Không thể giải ngân khoản tiền tạm giữ của đơn hàng.");

        public static readonly Error BankAccountNotVerified =
            new(
                "Payment.BankAccountNotVerified",
                "Bạn cần có tài khoản ngân hàng đã xác minh trước khi thanh toán.");
    }


    public static class NotificationErrors
    {
        public static readonly Error NotFound = new("NOTIFICATION_NOT_FOUND", "Không tìm thấy thông báo.");
    }

    public static class ShipmentErrors
    {
        public static readonly Error NotFound =
            new("Shipment.NotFound", "Không tìm thấy thông tin giao nhận của đơn hàng.");

        public static readonly Error Forbidden =
            new("Shipment.Forbidden", "Bạn không có quyền thực hiện thao tác này trên thông tin giao nhận.");

        public static readonly Error SellerReadyNotAllowed =
            new("Shipment.SellerReadyNotAllowed", "Trạng thái hiện tại không cho phép xác nhận đã chuẩn bị hàng.");

        public static readonly Error UnsupportedDeliveryMethod =
            new("Shipment.UnsupportedDeliveryMethod", "Phương thức giao nhận hiện tại không hỗ trợ xác nhận chuẩn bị hàng.");

        public static readonly Error OrderMismatch =
            new("Shipment.OrderMismatch", "Thông tin giao nhận không còn thuộc đơn hàng hiện tại.");

        public static readonly Error AlreadyExists =
            new("Shipment.AlreadyExists", "Đơn hàng đã có thông tin giao nhận.");
    }

    public static class DisputeCategoryErrors
    {
        public static readonly Error NotFound =
            new("DisputeCategory.NotFound", "Không tìm thấy loại tranh chấp.");

        public static readonly Error CodeAlreadyExists =
            new("DisputeCategory.CodeAlreadyExists", "Mã của loại tranh chấp đã tồn tại.");

        public static readonly Error Inactive =
            new("DisputeCategory.Inactive", "Loại tranh chấp này hiện đã bị vô hiệu hóa.");

        public static readonly Error TargetNotAllowed =
            new("DisputeCategory.TargetNotAllowed", "Loại tranh chấp không áp dụng cho đối tượng này.");
    }

    public static class AuditErrors
    {
        public static readonly Error NotFound = new("AuditLog.NotFound", "Không tìm thấy nhật ký hoạt động.");
        public static readonly Error InvalidDateRange = new("AuditLog.InvalidDateRange", "Thời gian bắt đầu (FromUtc) không được sau thời gian kết thúc (ToUtc).");
        public static Error InvalidFilter(string field) =>
            new("AuditLog.InvalidFilter", $"Bộ lọc '{field}' không hợp lệ.");
        public static Error FilterTooLong(string field, int maxLength) =>
            new("AuditLog.FilterTooLong", $"Bộ lọc '{field}' không được vượt quá {maxLength} ký tự.");
    }

    public static class SubscriptionPackageErrors
    {
        public static readonly Error NotFound =
            new("SubscriptionPackage.NotFound", "Không tìm thấy gói đăng ký.");

        public static readonly Error CodeAlreadyExists =
            new("SubscriptionPackage.CodeAlreadyExists", "Mã của gói đăng ký đã tồn tại.");

        public static readonly Error NameAlreadyExists =
            new("SubscriptionPackage.NameAlreadyExists", "Tên gói đăng ký đã tồn tại.");
        public static readonly Error Inactive =
            new("SubscriptionPackage.Inactive", "Gói đăng ký hiện không còn hoạt động.");

        public static Error InvalidEntitlement(string message) =>
            new("SubscriptionPackage.InvalidEntitlement", message);
    }

    public static class UserSubscriptionErrors
    {
        public static readonly Error InvalidSnapshot = new("UserSubscription.InvalidSnapshot", "Đăng ký gói thiếu bản ghi thông tin hợp lệ; cần đối soát trước khi kích hoạt.");
        public static readonly Error NotFound =
            new("UserSubscription.NotFound", "Không tìm thấy đăng ký gói.");

        public static readonly Error OpenSubscriptionExists =
            new("UserSubscription.OpenExists", "Bạn đang có đăng ký gói đang chờ xử lý hoặc đang hoạt động.");

        public static readonly Error RoleNotEligible =
            new("UserSubscription.RoleNotEligible", "Gói đăng ký không áp dụng cho loại tài khoản hiện tại.");

        public static readonly Error UserInactive =
            new("UserSubscription.UserInactive", "Tài khoản phải ở trạng thái đang hoạt động để mua gói đăng ký.");

        public static readonly Error InvalidStatus =
            new("UserSubscription.InvalidStatus", "Trạng thái đăng ký gói hiện tại không cho phép thao tác này.");

        public static readonly Error WalletNotFound =
            new("UserSubscription.WalletNotFound", "Không tìm thấy ví người dùng.");

        public static readonly Error InsufficientBalance =
            new("UserSubscription.InsufficientBalance", "Số dư khả dụng không đủ để mua gói đăng ký.");

        public static readonly Error PlatformRevenueWalletNotFound =
            new("UserSubscription.PlatformRevenueWalletNotFound", "Không tìm thấy ví doanh thu hệ thống.");
    }
}
