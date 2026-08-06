using AutoMapper;
using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Requests.Banks;
using HomeCycle.Application.DTOs.Requests.Brands;
using HomeCycle.Application.DTOs.Requests.Categories;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Offers;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Requests.Profiles;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Banks;
using HomeCycle.Application.DTOs.Responses.Brands;
using HomeCycle.Application.DTOs.Responses.Categories;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Negotiations;
using HomeCycle.Application.DTOs.Responses.Offers;
using HomeCycle.Application.DTOs.Responses.Posts;
using HomeCycle.Application.DTOs.Responses.Products;
using HomeCycle.Application.DTOs.Responses.Profiles;
using HomeCycle.Application.DTOs.Responses.Users;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ==================== AUTH / USER / PROFILE ====================

            CreateMap<RegisterPersonalRequest, user>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.Password, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsEmailVerified, opt => opt.Ignore());

            CreateMap<user, AuthResponse>();
            CreateMap<user, AuthUserDto>();

            CreateMap<user, PersonalProfileResponse>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            CreateMap<personal_profile, PersonalProfileResponse>()
                .ForMember(d => d.UserId, o => o.Ignore());

            CreateMap<bank_account, BankAccountDto>();

            CreateMap<UpdatePersonalProfileRequest, user>()
                .ForMember(d => d.PhoneNumber, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.PhoneNumber)))
                .ForMember(d => d.Username, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.Username)))
                .ForAllMembers(options => options.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdatePersonalProfileRequest, personal_profile>()
                .ForMember(d => d.FullName, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.FullName)))
                .ForAllMembers(options => options.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<UpdateAvatarRequest, user>()
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl != null));

            CreateMap<UpdateIdCardRequest, personal_profile>()
                .ForMember(d => d.VerificationStatus, o => o.Ignore())
                .ForMember(d => d.VerifiedBy, o => o.Ignore())
                .ForMember(d => d.VerifiedAt, o => o.Ignore())
                .ForMember(dest => dest.FrontIDCardImage, opt => opt.Ignore())
                .ForMember(dest => dest.BackIDCardImage, opt => opt.Ignore());

            CreateMap<UpdateBankAccountRequest, bank_account>()
                .ForMember(d => d.UserBankId, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore());

            // ==================== BUSINESS PROFILE ====================

            // Nhóm 1 - Định danh: blacklist-all rồi whitelist đúng field được phép sửa.
            CreateMap<UpdateIdentityRequest, business_profile>()
                .ForAllMembers(opts => opts.Ignore());

            CreateMap<UpdateIdentityRequest, business_profile>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName.Trim()))
                .ForMember(dest => dest.IdentityNumber, opt => opt.MapFrom(src => src.IdentityNumber.Trim()))
                .ForMember(dest => dest.IdentityName, opt => opt.MapFrom(src => src.IdentityName.Trim()))
                .ForMember(dest => dest.IdentityDob, opt => opt.MapFrom(src => src.IdentityDob))
                .ForMember(dest => dest.IdentityAddress, opt => opt.MapFrom(src => src.IdentityAddress.Trim()));

            // Nhóm 2 - Đăng ký kinh doanh: cùng nguyên tắc trên
            CreateMap<UpdateBusinessRegistrationRequest, business_profile>()
                .ForAllMembers(opts => opts.Ignore());

            CreateMap<UpdateBusinessRegistrationRequest, business_profile>()
                .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.BusinessName.Trim()))
                .ForMember(dest => dest.BusinessDescription, opt => opt.MapFrom(src => src.BusinessDescription == null ? null : src.BusinessDescription.Trim()))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => src.TaxCode.Trim()))
                .ForMember(dest => dest.BusinessAddress, opt => opt.MapFrom(src => src.BusinessAddress.Trim()))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Ward.Trim()))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Trim()))
                .ForMember(dest => dest.OperatingScope, opt => opt.MapFrom(src => src.OperatingScope == null ? null : src.OperatingScope.Trim()));

            // Create — nộp hồ sơ lần đầu: request -> business_profile (bỏ field set tay sau đó)
            CreateMap<SubmitBusinessProfileRequest, business_profile>()
                .ForMember(dest => dest.BusinessProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.ReputationScore, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RejectReason, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessName, opt => opt.MapFrom(src => src.BusinessName.Trim()))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName == null ? null : src.FullName.Trim()))
                .ForMember(dest => dest.BusinessDescription, opt => opt.MapFrom(src => src.BusinessDescription == null ? null : src.BusinessDescription.Trim()))
                .ForMember(dest => dest.TaxCode, opt => opt.MapFrom(src => src.TaxCode.Trim()))
                .ForMember(dest => dest.BusinessAddress, opt => opt.MapFrom(src => src.BusinessAddress.Trim()))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Ward.Trim()))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Trim()))
                .ForMember(dest => dest.IdentityNumber, opt => opt.MapFrom(src => src.IdentityNumber.Trim()))
                .ForMember(dest => dest.IdentityName, opt => opt.MapFrom(src => src.IdentityName.Trim()))
                .ForMember(dest => dest.IdentityAddress, opt => opt.MapFrom(src => src.IdentityAddress.Trim()))
                .ForMember(dest => dest.OperatingScope, opt => opt.MapFrom(src => src.OperatingScope == null ? null : src.OperatingScope.Trim()));


            // SubmitBusinessProfileRequest chứa cả field ngân hàng (BankCode/BankName/AccountNumber/AccountName)
            // -> map thẳng sang bank_account, AccountName cần .ToUpper() nên map riêng bằng MapFrom
            CreateMap<SubmitBusinessProfileRequest, bank_account>()
                .ForMember(dest => dest.UserBankId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.VerifyStatus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.BankCode, opt => opt.MapFrom(src => src.BankCode.Trim()))
                .ForMember(dest => dest.BankName, opt => opt.MapFrom(src => src.BankName.Trim()))
                .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber.Trim()))
                .ForMember(dest => dest.AccountName, opt => opt.MapFrom(src => src.AccountName.Trim().ToUpper()));

            // business_profile -> Response DTOs (base info dùng chung cho cả 2 response tổng hợp)
            CreateMap<business_profile, BusinessRegistrationDetailDto>();
            CreateMap<business_profile, BusinessProfileDetailDto>();

            // user -> BusinessProfileDetailDto (map trước, business_profile map đè sau — giống PersonalProfileService)
            CreateMap<user, BusinessProfileDetailDto>();

            // bank_account -> map đè lên response tổng hợp
            CreateMap<bank_account, BusinessRegistrationDetailDto>();
            CreateMap<bank_account, BankAccountDto>();   

            // ==================== BUSINESS DOCUMENT ====================

            CreateMap<business_document, BusinessRegistrationDocumentDto>();
            CreateMap<business_document, BusinessDocumentResponseDto>();

            CreateMap<BusinessDocumentDto, business_document>()
                .ForMember(dest => dest.DocumentUrl, opt => opt.Ignore());

            // ==================== BUSINESS SERVICE AREA ====================

            CreateMap<business_service_area, BusinessRegistrationServiceAreaDto>();
            CreateMap<business_service_area, BusinessServiceAreaResponseDto>();

            // QUAN TRỌNG - 2 map này TRƯỚC ĐÂY BỊ THIẾU HOÀN TOÀN trong file, trong khi service
            // đang gọi _mapper.Map<business_service_area>(sa) cho cả 2 DTO này. AutoMapper bắt buộc
            // phải có CreateMap khai báo tường minh, nếu không sẽ ném AutoMapperMappingException
            // ngay lúc runtime khi SubmitBusinessProfileAsync hoặc UpdateBusinessServiceAreasAsync
            // được gọi với ServiceAreas không rỗng.
            CreateMap<BusinessServiceAreaDto, business_service_area>()
                .ForMember(dest => dest.BusinessServiceAreaId, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.Priority, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Trim()))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District.Trim()))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Ward.Trim()));

            CreateMap<BusinessServiceAreaRequestDto, business_service_area>()
                .ForMember(dest => dest.BusinessServiceAreaId, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.Priority, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City.Trim()))
                .ForMember(dest => dest.District, opt => opt.MapFrom(src => src.District.Trim()))
                .ForMember(dest => dest.Ward, opt => opt.MapFrom(src => src.Ward.Trim()));

            // ==================== PROCUREMENT PREFERENCE / SURVEY ====================

            CreateMap<SubmitBusinessSurveyRequest, business_procurement_preference>()
                .ForMember(dest => dest.PreferenceId, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessProfileId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<business_procurement_preference, BusinessSurveyDetailResponse>();

            // ==================== CATEGORY ====================

            CreateMap<category, CategoryResponse>();

            CreateMap<CreateCategoryRequest, category>()
                .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName.Trim()))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(src => src.Description == null
                        ? null
                        : src.Description.Trim()));

            CreateMap<UpdateCategoryRequest, category>()
                .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName.Trim()))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(src => src.Description == null
                        ? null
                        : src.Description.Trim()));

            // ==================== BRAND ====================

            CreateMap<brand, BrandResponse>();
            CreateMap<CreateBrandRequest, brand>()
                .ForMember(dest => dest.BrandId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.BrandName.Trim()))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(
                        src => src.Description == null
                            ? null
                            : src.Description.Trim()));

            CreateMap<UpdateBrandRequest, brand>()
                .ForMember(dest => dest.BrandId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.BrandName, opt => opt.MapFrom(src => src.BrandName.Trim()))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(
                        src => src.Description == null
                            ? null
                            : src.Description.Trim()));

            // ==================== PRODUCT TYPE — AGGREGATE ROOT + Attribute+Option ====================

            CreateMap<product_type, ProductTypeResponse>();

            CreateMap<product_type, ProductTypeDetailResponse>()
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.ProductAttributes));

            CreateMap<product_attribute, ProductAttributeResponse>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.ProductAttributeOptions));

            CreateMap<product_attribute_option, ProductAttributeOptionResponse>();

            CreateMap<CreateProductTypeRequest, product_type>()
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.ProductAttributes, opt => opt.Ignore());

            // Attribute lồng trong Aggregate Create/Update ProductType (KHÁC với CRUD lẻ bên dưới)
            CreateMap<CreateAttributeRequest, product_attribute>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeName, opt => opt.MapFrom(src => src.AttributeName.Trim()))
                .ForMember(dest => dest.Unit,
                    opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Unit) ? null : src.Unit.Trim()));

            CreateMap<UpdateProductTypeRequest, product_type>()
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProductAttributes, opt => opt.Ignore());

            CreateMap<UpdateAttributeRequest, product_attribute>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore());

            // Option lồng trong Aggregate Create/Update ProductType (KHÁC với CRUD lẻ bên dưới)
            CreateMap<CreateAttributeOptionRequest, product_attribute_option>()
                .ForMember(dest => dest.OptionId, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.OptionValue, opt => opt.MapFrom(src => src.OptionValue.Trim()));

            // ==================== PRODUCT ATTRIBUTE — CRUD LẺ (ProductAttributeService) ====================

            CreateMap<product_attribute, ProductAttributeResponse>()
                .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.ProductAttributeOptions));

            // CẦN XÁC NHẬN: tên DTO đúng CreateProductAttributeRequest/UpdateProductAttributeRequest
            // (dùng bởi IProductAttributeService, KHÔNG PHẢI CreateAttributeRequest ở trên).
            CreateMap<CreateAttributeRequest, product_attribute>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeName, opt => opt.MapFrom(src => src.AttributeName.Trim()))
                .ForMember(dest => dest.Unit,
                    opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Unit) ? null : src.Unit.Trim()));

            CreateMap<UpdateAttributeRequest, product_attribute>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeName, opt => opt.MapFrom(src => src.AttributeName.Trim()))
                .ForMember(dest => dest.Unit,
                    opt => opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Unit) ? null : src.Unit.Trim()));

            // ==================== PRODUCT ATTRIBUTE OPTION — CRUD LẺ ====================

            CreateMap<product_attribute_option, ProductAttributeOptionResponse>();

            // CẦN XÁC NHẬN: tên DTO đúng CreateProductAttributeOptionRequest/UpdateProductAttributeOptionRequest
            CreateMap<CreateAttributeOptionRequest, product_attribute_option>()
                .ForMember(dest => dest.OptionId, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.OptionValue, opt => opt.MapFrom(src => src.OptionValue.Trim()));

            CreateMap<UpdateAttributeOptionRequest, product_attribute_option>()
                .ForMember(dest => dest.OptionId, opt => opt.Ignore())
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.OptionValue, opt => opt.MapFrom(src => src.OptionValue.Trim()));

            // ==================== MEDIA ====================

            CreateMap<media, MediaResponse>();
            CreateMap<MediaRequest, media>();

            // ==================== POST (Sell / Buy) ====================

            CreateMap<CreatePostRequest, post>()
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.PostType, opt => opt.Ignore())
                .ForMember(dest => dest.BasePrice, opt => opt.Ignore())
                .ForMember(dest => dest.RemainingQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                // Chỉ include các DTO kế thừa CreatePostRequest
                .Include<CreateSellPostRequest, post>()
                .Include<CreateBuyPostRequest, post>();

            CreateMap<CreateSellPostRequest, post>();
            CreateMap<CreateBuyPostRequest, post>();

            CreateMap<UpdatePostRequest, post>()
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.PostType, opt => opt.Ignore())
                .ForMember(dest => dest.BasePrice, opt => opt.Ignore())
                .ForMember(dest => dest.RemainingQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                //.ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null))
                .Include<UpdateSellPostRequest, post>()
                .Include<UpdateBuyPostRequest, post>();

            CreateMap<UpdateSellPostRequest, post>();
            CreateMap<UpdateBuyPostRequest, post>();

            //CreateMap<post, PostResponse>();

            CreateMap<post, PostDetailResponse>()
                .IncludeBase<post, PostResponse>()
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.Medias, opt => opt.Ignore());

            CreateMap<CreateSellPostRequest, post>()
                .ForMember(dest => dest.PostType, opt => opt.Ignore())
                .ForMember(x => x.BasePrice, opt => opt.Ignore());

            CreateMap<CreateBuyPostRequest, post>()
                .ForMember(x => x.PostType, opt => opt.Ignore())
                .ForMember(x => x.BasePrice, opt => opt.Ignore());

            CreateMap<UpdateSellPostRequest, post>()
                .IncludeBase<UpdatePostRequest, post>()
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.PostType, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.RemainingQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(x => x.BasePrice, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<UpdateBuyPostRequest, post>()
                .ForMember(x => x.PostType, opt => opt.Ignore())
                .ForMember(x => x.BasePrice, opt => opt.Ignore());

            CreateMap<ProductRequest, product>()
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.PostId, opt => opt.Ignore());

            CreateMap<post, PostResponse>();
            CreateMap<post, PostDetailResponse>();

            CreateMap<post, PostResponse>()
                .ForMember(d => d.ProductId,
                    o => o.MapFrom(s =>
                        s.Product == null
                            ? Guid.Empty
                            : s.Product.ProductId))
                .ForMember(d => d.ProductName,
                    o => o.MapFrom(s =>
                        s.Product == null
                            ? null
                            : s.Product.ProductName))
                .ForMember(d => d.ProductTypeName,
                    o => o.MapFrom(s =>
                        s.Product == null
                            ? null
                            : s.Product.ProductTypeName))
                .ForMember(d => d.CategoryName,
                    o => o.MapFrom(s =>
                        s.Product == null
                            ? null
                            : s.Product.CategoryName))
                .ForMember(d => d.BrandName,
                    o => o.MapFrom(s =>
                        s.Product == null
                            ? null
                            : s.Product.BrandName))
                .ForMember(d => d.Medias, o => o.Ignore());

            CreateMap<PagedResult<post>, PagedResult<PostResponse>>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));

            // ==================== PRODUCT (dùng chung cho Sell/Buy, gắn liền Post) ====================

            CreateMap<ProductRequest, product>()
                .ForMember(d => d.ProductId, o => o.Ignore())
                .ForMember(d => d.PostId, o => o.Ignore())
                .ForMember(d => d.CategoryName, o => o.Ignore())
                .ForMember(d => d.ProductTypeName, o => o.Ignore())
                .ForMember(d => d.BrandName, o => o.Ignore())
                .ForMember(d => d.Product_Attribute_Values, o => o.Ignore());

            CreateMap<product, ProductResponse>()
                .ForMember(dest => dest.AttributeValues, opt => opt.MapFrom(src => src.Product_Attribute_Values));
            CreateMap<product_attribute_value, ProductAttributeValueResponse>();

            CreateMap<ProductRequirementRequest, product>()
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                // Đã chốt từ trước: ExpectedPrice (Buy) dùng chung cột OriginalPrice.
                .ForMember(dest => dest.OriginalPrice, opt => opt.MapFrom(src => src.ExpectedPrice));

            CreateMap<ProductRequirementRequest, product>()
                .ForMember(x => x.ProductId, opt => opt.Ignore())
                .ForMember(x => x.PostId, opt => opt.Ignore());

            CreateMap<ProductRequest, product>()
                .ForMember(x => x.ProductId, opt => opt.Ignore())
                .ForMember(x => x.PostId, opt => opt.Ignore());

            CreateMap<CreateAttributeRequest, product_attribute>()
                .ForMember(destination => destination.AttributeId, option => option.Ignore())
                .ForMember(destination => destination.ProductTypeId, option => option.Ignore())
                .ForMember(destination => destination.AttributeName,
                    option => option.MapFrom(source => source.AttributeName.Trim()))
                .ForMember(destination => destination.Unit,
                    option => option.MapFrom(source =>
                        string.IsNullOrWhiteSpace(source.Unit)
                            ? null
                            : source.Unit.Trim()));
            CreateMap<UpdateAttributeRequest, product_attribute>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore());

            CreateMap<CreateAttributeOptionRequest, product_attribute_option>()
                .ForMember(destination => destination.OptionId, option => option.Ignore())
                .ForMember(destination => destination.AttributeId, option => option.Ignore())
                .ForMember(destination => destination.OptionValue,
                    option => option.MapFrom(source => source.OptionValue.Trim()));

            CreateMap<product_attribute, ProductAttributeResponse>();
            CreateMap<product_attribute_option, ProductAttributeOptionResponse>();

            // product -> ProductResponse: map danh sách AttributeValues (navigation được nạp ở repository)
            // (CreateMap<product, ProductResponse> đã khai báo ở phần PRODUCT phía trên)

            // ==================== OFFER / NEGOTIATION / MESSAGE ====================

            CreateMap<offer, OfferResponse>();

            CreateMap<CreateOfferRequest, offer>()
                .ForMember(dest => dest.OfferId, opt => opt.Ignore())
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                .ForMember(dest => dest.SenderId, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiverId, opt => opt.Ignore())
                .ForMember(dest => dest.OfferStatus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<offer, OfferResponse>();
            CreateMap<offer, OfferListItem>()
                .ForMember(dest => dest.OfferId, opt => opt.MapFrom(src => src.OfferId)) // hoặc src.OfferId
                .ForMember(dest => dest.OfferStatus, opt => opt.MapFrom(src => src.OfferStatus.ToString()))
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.Username : string.Empty))
                .ForMember(dest => dest.SenderAvatarUrl, opt => opt.MapFrom(src => src.Sender != null ? src.Sender.AvatarUrl : null))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Receiver != null ? src.Receiver.Username : string.Empty))
                .ForMember(dest => dest.ReceiverAvatarUrl, opt => opt.MapFrom(src => src.Receiver != null ? src.Receiver.AvatarUrl : null));

            CreateMap<UpdateOfferRequest, offer>()
                .ForMember(dest => dest.OfferId, opt => opt.Ignore())
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                .ForMember(dest => dest.SenderId, opt => opt.Ignore())
                .ForMember(dest => dest.ReceiverId, opt => opt.Ignore())
                .ForMember(dest => dest.OfferStatus, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<offer, negotiation>()
                .ForMember(dest => dest.NegotiationId, opt => opt.Ignore())
                .ForMember(dest => dest.PostId, opt => opt.MapFrom(src => src.PostId))
                .ForMember(dest => dest.OfferId, opt => opt.MapFrom(src => src.OfferId))
                .ForMember(dest => dest.SellerId, opt => opt.MapFrom(src => src.ReceiverId))
                .ForMember(dest => dest.BuyerId, opt => opt.MapFrom(src => src.SenderId))
                .ForMember(dest => dest.FinalPrice, opt => opt.Ignore())
                .ForMember(dest => dest.FinalQuantity, opt => opt.Ignore())
                .ForMember(dest => dest.LastMessageAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.NegotiationStatus, opt => opt.Ignore());

            CreateMap<offer, message>()
                .ForMember(dest => dest.MessageId, opt => opt.Ignore())
                .ForMember(dest => dest.NegotiationId, opt => opt.Ignore())
                .ForMember(dest => dest.SenderId, opt => opt.MapFrom(src => src.SenderId))
                .ForMember(dest => dest.OfferPrice, opt => opt.MapFrom(src => src.OfferPrice))
                .ForMember(dest => dest.OfferQuantity, opt => opt.MapFrom(src => src.OfferQuantity))
                .ForMember(dest => dest.MessageContent, opt => opt.Ignore())
                .ForMember(dest => dest.MessageType, opt => opt.Ignore())
                .ForMember(dest => dest.OfferStatus, opt => opt.Ignore())
                .ForMember(dest => dest.MediaUrl, opt => opt.Ignore())
                .ForMember(dest => dest.IsRead, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.BasePriceSnapshot, opt => opt.Ignore());

            CreateMap<negotiation, NegotiationResponse>();
            CreateMap<message, MessageResponse>();
        }
    }
}
