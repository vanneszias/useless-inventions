using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Useless_Inventions.Models;

public class Follow
{
    public int Id { get; set; }

    [Required]
    public string FollowerId { get; set; } = string.Empty;
    public ApplicationUser Follower { get; set; } = null!;

    [Required]
    public string FolloweeId { get; set; } = string.Empty;
    public ApplicationUser Followee { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
} 