using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.ViewComponents;

public class FeedViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    public FeedViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentUserId = _userManager.GetUserId(HttpContext.User);
        var followedUserIds = await _context.Follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FolloweeId)
            .ToListAsync();
        followedUserIds.Add(currentUserId); // include own posts
        var inventions = await _context.Inventions
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
            .Where(i => followedUserIds.Contains(i.UserId))
            .OrderByDescending(i => i.CreatedAt)
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
            IsLiked = i.Likes != null && i.Likes.Any(l => l.UserId == currentUserId),
            AvatarUrl = i.User?.AvatarUrl
        }).ToList();

        // Create new invention with form data if available (for error redisplay)
        var newInvention = new Invention();
        if (TempData.Peek("FormData") is Dictionary<string, string> formData)
        {
            newInvention.Title = formData.GetValueOrDefault("Title", "");
            newInvention.Description = formData.GetValueOrDefault("Description", "");
            newInvention.ImageUrl = formData.GetValueOrDefault("ImageUrl", "");
        }

        var feedViewModel = new FeedViewModel {
            Posts = postModels,
            NewInvention = newInvention
        };

        // Pass error and success messages to the view
        if (TempData.Peek("ErrorMessages") is List<string> errorMessages)
        {
            ViewBag.ErrorMessages = errorMessages;
            TempData.Remove("ErrorMessages"); // Remove after reading
        }
        
        if (TempData.Peek("SuccessMessage") is string successMessage)
        {
            ViewBag.SuccessMessage = successMessage;
            TempData.Remove("SuccessMessage"); // Remove after reading
        }
        
        if (TempData.Peek("FormData") != null)
        {
            TempData.Remove("FormData"); // Remove after reading
        }

        return View("_Feed", feedViewModel);
    }
} 