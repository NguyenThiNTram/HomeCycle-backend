using AutoMapper;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Requests.Users;
using HomeCycle.Application.DTOs.Responses.Auths;
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
            .ForMember(dest => dest.UserId,
                opt => opt.Ignore())
            .ForMember(dest => dest.Password,
                opt => opt.Ignore())
            .ForMember(dest => dest.Role,
                opt => opt.Ignore())
            .ForMember(dest => dest.Status,
                opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt,
                opt => opt.Ignore())
            .ForMember(dest => dest.IsEmailVerified,
                opt => opt.Ignore());

            CreateMap<user, AuthResponse>();
            CreateMap<user, AuthUserDto>();

            CreateMap<user, PersonalProfileResponse>()
            .ForMember(d => d.Role, o => o.MapFrom(s => s.Role.ToString()))
            .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));

            CreateMap<personal_profile, PersonalProfileResponse>()
                .ForMember(d => d.UserId, o => o.Ignore());

            CreateMap<bank_account, BankAccountDto>();

            CreateMap<UpdatePersonalProfileRequest, user>()
                .ForAllMembers(opt => opt.Condition((_, srcMember) => srcMember != null));
            CreateMap<UpdatePersonalProfileRequest, user>()
                .ForMember(d => d.PhoneNumber, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.PhoneNumber)))
                .ForMember(d => d.Username, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.Username)));

            CreateMap<UpdatePersonalProfileRequest, personal_profile>()
                .ForMember(d => d.FullName, o => o.Condition(s => !string.IsNullOrWhiteSpace(s.FullName)));
            CreateMap<UpdatePersonalProfileRequest, personal_profile>()
                .ForAllMembers(opt => opt.Condition((_, srcMember) => srcMember != null));

            CreateMap<UpdateAvatarRequest, user>()
                .ForMember(d => d.AvatarUrl, o => o.MapFrom(s => s.AvatarUrl.Trim()));

            CreateMap<UpdateIdCardRequest, personal_profile>();
            CreateMap<UpdateIdCardRequest, personal_profile>()
                .ForMember(d => d.VerificationStatus, o => o.Ignore())
                .ForMember(d => d.VerifiedBy, o => o.Ignore())
                .ForMember(d => d.VerifiedAt, o => o.Ignore());

            CreateMap<UpdateBankAccountRequest, bank_account>()
                .ForMember(d => d.UserBankId, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore());
        }
    }
}
