using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.DTOs.Responses.Users
{
    public class PublicUserProfileResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int ReputationScore { get; set; }
        //public bool IsVerified { get; set; }
        public DateTime JoinedAt { get; set; }
        public int ActivePostCount { get; set; }
    }

    public class PersonalPublicProfileResponse : PublicUserProfileResponse
    {
        public string? FullName { get; set; }
    }

    public class BusinessPublicProfileResponse : PublicUserProfileResponse
    {
        public string? BusinessName { get; set; }
        public string? BusinessDescription { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public string? OperatingScope { get; set; }
        public string? BusinessModel { get; set; }
    }
}
