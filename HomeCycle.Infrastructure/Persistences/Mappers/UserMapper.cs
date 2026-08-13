using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class UserMapper
    {
        public static user? ToDomain(this User entity)
        {
            if (entity == null) return null;
            return new user
            {
                UserId = entity.UserId,
                Username = entity.Username,
                Email = entity.Email,
                IsEmailVerified = entity.IsEmailVerified,
                Password = entity.Password,
                PhoneNumber = entity.PhoneNumber,
                AvatarUrl = entity.AvatarUrl,
                Role = (UserRole)entity.Role, //ép kiểu lấy giá trị enum từ int
                Status = (UserStatus)entity.Status, //ép kiểu lấy giá trị enum từ int
                CreatedAt = entity.CreatedAt
            };
        }
        public static User ToInfrastructure(this user entity)
        {
            if (entity == null) return null;
            return new User
            {
                UserId = entity.UserId,
                Username = entity.Username,
                Email = entity.Email,
                IsEmailVerified = entity.IsEmailVerified,
                Password = entity.Password,
                PhoneNumber = entity.PhoneNumber,
                AvatarUrl = entity.AvatarUrl,
                Role = (int)entity.Role, //ép kiểu lấy giá trị int từ enum
                Status = (int)entity.Status, //ép kiểu lấy giá trị int từ enum
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
