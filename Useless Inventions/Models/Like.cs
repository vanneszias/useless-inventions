using Microsoft.AspNetCore.Identity;

namespace Useless_Inventions.Models;

public class Like
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public int InventionId { get; set; }
    public Invention Invention { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public IdentityUser User { get; set; } = null!;
}