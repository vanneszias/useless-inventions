using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using System.Text.RegularExpressions;

namespace Useless_Inventions.ViewComponents;

public class TrendsViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    public TrendsViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var inventions = await _context.Inventions
            .Where(i => i.CreatedAt >= since)
            .Include(i => i.Likes)
            .ToListAsync();
        var hashtags = inventions
            .SelectMany(i => Regex.Matches(i.Title + " " + i.Description, "#\\w+").Select(m => m.Value))
            .GroupBy(tag => tag)
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToList();
        return View("_TrendsList", hashtags);
    }
} 