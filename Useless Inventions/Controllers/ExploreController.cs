using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.Controllers
{
    public class ExploreController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExploreController> _logger;

        public ExploreController(ApplicationDbContext context, ILogger<ExploreController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Redirect to Trends as the default explore page
            return RedirectToAction(nameof(Trends));
        }

        public async Task<IActionResult> Trends()
        {
            ViewData["Title"] = "Explore Trends";
            
            try
            {
                // Get trending inventions (most liked in the last 7 days)
                var trendingInventions = await _context.Inventions
                    .Include(i => i.User)
                    .Include(i => i.Likes)
                    .Include(i => i.Comments)
                    .Where(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                    .OrderByDescending(i => i.Likes.Count)
                    .ThenByDescending(i => i.Comments.Count)
                    .ThenByDescending(i => i.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                // Get trending hashtags (mock data for now - could be extracted from invention descriptions)
                var trendingHashtags = new List<string>
                {
                    "#UselessInventions",
                    "#Innovation",
                    "#Creative",
                    "#Funny",
                    "#Weird",
                    "#Genius",
                    "#DIY",
                    "#Tech",
                    "#Design",
                    "#Humor"
                };

                ViewBag.TrendingHashtags = trendingHashtags;
                return View(trendingInventions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trends page");
                ViewBag.ErrorMessage = "Unable to load trending content at this time.";
                return View(new List<Invention>());
            }
        }

        public async Task<IActionResult> People()
        {
            ViewData["Title"] = "Explore People";
            
            try
            {
                // Get most active users (users with most inventions)
                var activeUsers = await _context.Users
                    .Include(u => u.Inventions)
                    .Where(u => u.Inventions.Any())
                    .OrderByDescending(u => u.Inventions.Count)
                    .ThenBy(u => u.UserName)
                    .Take(20)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.AvatarUrl,
                        InventionCount = u.Inventions.Count,
                        LatestInvention = u.Inventions.OrderByDescending(i => i.CreatedAt).FirstOrDefault(),
                        JoinedDate = u.Id // Using Id as a proxy for join date since we don't have a created date
                    })
                    .ToListAsync();

                // Get recently joined users
                var newUsers = await _context.Users
                    .OrderByDescending(u => u.Id) // Assuming newer users have higher IDs
                    .Take(10)
                    .Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.AvatarUrl,
                        InventionCount = u.Inventions.Count(),
                        IsNewUser = u.Inventions.Count() == 0
                    })
                    .ToListAsync();

                ViewBag.ActiveUsers = activeUsers;
                ViewBag.NewUsers = newUsers;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading people page");
                ViewBag.ErrorMessage = "Unable to load people at this time.";
                return View();
            }
        }
    }
}