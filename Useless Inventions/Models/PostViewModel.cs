namespace Useless_Inventions.Models;

public class PostViewModel
{
    public int Id { get; set; }
    public string User { get; set; } = "";
    public string Content { get; set; } = "";
    public string Time { get; set; } = "";
    public int Likes { get; set; }
    public int Comments { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsLiked { get; set; }
    public string? AvatarUrl { get; set; }
}

public class FeedViewModel
{
    public List<PostViewModel> Posts { get; set; } = new();
    public Invention NewInvention { get; set; } = new();
} 