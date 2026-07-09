using AutoMapper;
using HomeCycle.Application.DTOs.Requests.Auths;
using HomeCycle.Application.DTOs.Responses;
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
                opt => opt.Ignore())      // Password sẽ được hash trong Service
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
        }
    }
}
