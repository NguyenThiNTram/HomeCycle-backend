using AutoMapper;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Requests.Brands;
using HomeCycle.Application.DTOs.Requests.Categories;
using HomeCycle.Application.DTOs.Requests.Media;
using HomeCycle.Application.DTOs.Requests.Posts;
using HomeCycle.Application.DTOs.Requests.Products;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Brands;
using HomeCycle.Application.DTOs.Responses.Categories;
using HomeCycle.Application.DTOs.Responses.Media;
using HomeCycle.Application.DTOs.Responses.Products;
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
                .ForAllMembers(options => options.Condition((src, dest, srcMember) => srcMember != null));
            CreateMap<UpdatePersonalProfileRequest, user>()
                .ForMember(d => d.PhoneNumber, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.PhoneNumber)))
                .ForMember(d => d.Username, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.Username)));

            CreateMap<UpdatePersonalProfileRequest, personal_profile>()
                .ForMember(d => d.FullName, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.FullName)));
            CreateMap<UpdatePersonalProfileRequest, personal_profile>()
                .ForAllMembers(options => options.Condition((src, dest, srcMember) => srcMember != null));


            CreateMap<UpdateAvatarRequest, user>()
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl != null));

            CreateMap<UpdateIdCardRequest, personal_profile>();
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

            CreateMap<UpdateProductTypeRequest, product_type>()
                .ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProductAttributes, opt => opt.Ignore());

            CreateMap<media, MediaResponse>();

            CreateMap<MediaRequest, media>();

            CreateMap<CreatePostRequest, post>()
                .ForMember(x => x.PostId, opt => opt.Ignore())
                .ForMember(x => x.OwnerId, opt => opt.Ignore())
                .ForMember(x => x.CreatedAt, opt => opt.Ignore())
                .ForMember(x => x.UpdatedAt, opt => opt.Ignore())
                .ForMember(x => x.RemainingQuantity, opt => opt.Ignore())
                .ForMember(x => x.Status, opt => opt.Ignore());


            CreateMap<ProductRequest, product>()
                .ForMember(dest => dest.ProductId, opt => opt.Ignore())
                .ForMember(dest => dest.PostId, opt => opt.Ignore());
            //.ForMember(dest => dest.CategoryId, opt => opt.Ignore())
            //.ForMember(dest => dest.PostId, opt => opt.Ignore())
            //.ForMember(dest => dest.ProductTypeId, opt => opt.Ignore())
            //.ForMember(dest => dest.BrandId, opt => opt.Ignore());

            CreateMap<ProductRequirementRequest, product>()
                .ForMember(x => x.ProductId, opt => opt.Ignore())
                .ForMember(x => x.PostId, opt => opt.Ignore());

            CreateMap<ProductRequest, product>()
                .ForMember(x => x.ProductId, opt => opt.Ignore())
                .ForMember(x => x.PostId, opt => opt.Ignore());

            //CreateMap<UpdatePostRequest, post>()
            //    .ForMember(x => x.PostId, opt => opt.Ignore())
            //    .ForMember(x => x.OwnerId, opt => opt.Ignore())
            //    .ForMember(dest => dest.PostType, opt => opt.Ignore())
            //    .ForMember(x => x.CreatedAt, opt => opt.Ignore())
            //    .ForMember(x => x.UpdatedAt, opt => opt.Ignore())
            //    .ForMember(x => x.Status, opt => opt.Ignore())
            //    .ForMember(x => x.IsBusinessPosting, opt => opt.Ignore())
            //    .ForMember(x => x.RemainingQuantity, opt => opt.Ignore());


        }
    }
}
