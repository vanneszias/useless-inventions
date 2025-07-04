using System;

namespace Useless_Inventions.Models
{
    public class UserProfileCardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int? InventionCount { get; set; }
        public bool ShowFollowButton { get; set; } = false;
        public bool IsCurrentUser { get; set; } = false;
        public bool IsFollowing { get; set; } = false;
    }
} 