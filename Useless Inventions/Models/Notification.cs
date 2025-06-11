using System.ComponentModel.DataAnnotations;

namespace Useless_Inventions.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string ToUserId { get; set; } = string.Empty;

        public string? FromUserId { get; set; }

        [Required]
        public NotificationType Type { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        public int? InventionId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; } = false;

        // Navigation properties
        public ApplicationUser ToUser { get; set; } = null!;
        public ApplicationUser? FromUser { get; set; }
        public Invention? Invention { get; set; }
    }

    public enum NotificationType
    {
        Like = 1,
        Comment = 2,
        Follow = 3,
        Mention = 4,
        NewInvention = 5
    }
}