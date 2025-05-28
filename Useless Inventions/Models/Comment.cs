using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Useless_Inventions.Models;

public class Comment
{
    public int Id { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public int InventionId { get; set; }
    public Invention Invention { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}