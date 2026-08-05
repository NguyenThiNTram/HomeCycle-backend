using HomeCycle.Application.Commons.Results;
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

        public static readonly Error Forbidden = new("POST_FORBIDDEN", "You do not have permission to access this post.");

        public static readonly Error PostExpired = new("POST_EXPIRED", "Bài đăng đã hết thời hạn cho phép chỉnh sửa.");

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
    }
}
