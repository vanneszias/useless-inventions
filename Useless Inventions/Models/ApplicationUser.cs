using Microsoft.AspNetCore.Identity;

namespace Useless_Inventions.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
    }
} 