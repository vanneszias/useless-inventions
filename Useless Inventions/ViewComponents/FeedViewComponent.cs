using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.ViewComponents;

public class FeedViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    public FeedViewComponent(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentUserId = _userManager.GetUserId(HttpContext.User);
        var inventions = await _context.Inventions
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .OrderByDescending(i => i.Likes.Count)
            .ThenByDescending(i => i.Comments.Count)
            .Take(20)
            .ToListAsync();
        var postModels = inventions.Select(i => new PostViewModel {
            Id = i.Id,
            User = "@" + (i.User?.UserName ?? "Unknown"),
            Content = i.Title + ": " + (i.Description.Length > 150 ? i.Description.Substring(0, 147) + "..." : i.Description),
            Time = i.CreatedAt.ToString("MMM dd, yyyy"),
            Likes = i.Likes?.Count ?? 0,
            Comments = i.Comments?.Count ?? 0,
            ImageUrl = i.ImageUrl,
            IsLiked = i.Likes != null && i.Likes.Any(l => l.UserId == currentUserId)
        }).ToList();
        var feedViewModel = new FeedViewModel {
            Posts = postModels,
            NewInvention = new Invention()
        };
        return View("_Feed", feedViewModel);
    }
} 