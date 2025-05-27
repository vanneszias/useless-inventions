using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Useless_Inventions.Data;

namespace Useless_Inventions.ViewComponents;

public class SuggestionsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    public SuggestionsViewComponent(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentUserId = _userManager.GetUserId(HttpContext.User);
        var users = await _context.Inventions
            .Where(i => i.UserId != currentUserId)
            .GroupBy(i => i.User)
            .Select(g => new { User = g.Key, InventionCount = g.Count() })
            .OrderByDescending(g => g.InventionCount)
            .Take(5)
            .ToListAsync();
        return View("_SuggestionsList", users);
    }
} 