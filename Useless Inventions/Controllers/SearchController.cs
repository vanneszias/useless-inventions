using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SearchController> _logger;

        public SearchController(ApplicationDbContext context, ILogger<SearchController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult Index(string? query = null)
        {
            ViewData["Title"] = "Search";
            ViewBag.Query = query;
            
            if (string.IsNullOrWhiteSpace(query))
            {
                // Show empty search state with suggestions
                ViewBag.ShowSuggestions = true;
                return View(new SearchResultsViewModel());
            }

            return RedirectToAction(nameof(Results), new { query = query });
        }

        public async Task<IActionResult> Results(string query, string type = "all", int page = 1)
        {
            ViewData["Title"] = $"Search results for \"{query}\"";
            ViewBag.Query = query;
            ViewBag.SearchType = type;
            ViewBag.CurrentPage = page;

            const int pageSize = 20;
            var results = new SearchResultsViewModel();

            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var normalizedQuery = query.ToLower().Trim();

                // Search inventions
                if (type == "all" || type == "inventions")
                {
                    var inventionsQuery = _context.Inventions
                        .Include(i => i.User)
                        .Include(i => i.Likes)
                        .Include(i => i.Comments)
                        .Where(i => i.Title.ToLower().Contains(normalizedQuery) || 
                                   i.Description.ToLower().Contains(normalizedQuery))
                        .OrderByDescending(i => i.CreatedAt);

                    results.TotalInventions = await inventionsQuery.CountAsync();
                    
                    if (type == "inventions")
                    {
                        results.Inventions = await inventionsQuery
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();
                    }
                    else
                    {
                        results.Inventions = await inventionsQuery.Take(5).ToListAsync();
                    }
                }

                // Search users
                if (type == "all" || type == "people")
                {
                    var usersQuery = _context.Users
                        .Where(u => u.UserName!.ToLower().Contains(normalizedQuery) || 
                                   (u.Email != null && u.Email.ToLower().Contains(normalizedQuery)))
                        .OrderBy(u => u.UserName);

                    results.TotalUsers = await usersQuery.CountAsync();
                    
                    if (type == "people")
                    {
                        var users = await usersQuery
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .Select(u => new
                            {
                                u.Id,
                                u.UserName,
                                u.Email,
                                u.AvatarUrl,
                                InventionCount = u.Inventions.Count()
                            })
                            .ToListAsync();
                        
                        ViewBag.Users = users;
                    }
                    else
                    {
                        var users = await usersQuery
                            .Take(3)
                            .Select(u => new
                            {
                                u.Id,
                                u.UserName,
                                u.Email,
                                u.AvatarUrl,
                                InventionCount = u.Inventions.Count()
                            })
                            .ToListAsync();
                        
                        ViewBag.Users = users;
                    }
                }

                // Calculate pagination info
                if (type == "inventions")
                {
                    ViewBag.TotalPages = (int)Math.Ceiling((double)results.TotalInventions / pageSize);
                }
                else if (type == "people")
                {
                    ViewBag.TotalPages = (int)Math.Ceiling((double)results.TotalUsers / pageSize);
                }

                return View(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing search for query: {Query}", query);
                ViewBag.ErrorMessage = "An error occurred while searching. Please try again.";
                return View(new SearchResultsViewModel());
            }
        }

        [HttpPost]
        public IActionResult QuickSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Results), new { query = query.Trim() });
        }

        // API endpoint for autocomplete suggestions
        [HttpGet]
        public async Task<IActionResult> Suggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new { suggestions = new string[0] });
            }

            try
            {
                var normalizedTerm = term.ToLower().Trim();

                // Get invention title suggestions
                var inventionSuggestions = await _context.Inventions
                    .Where(i => i.Title.ToLower().Contains(normalizedTerm))
                    .Select(i => i.Title)
                    .Distinct()
                    .Take(5)
                    .ToListAsync();

                // Get user suggestions
                var userSuggestions = await _context.Users
                    .Where(u => u.UserName!.ToLower().Contains(normalizedTerm))
                    .Select(u => u.UserName!)
                    .Take(3)
                    .ToListAsync();

                var allSuggestions = inventionSuggestions
                    .Concat(userSuggestions)
                    .Take(8)
                    .ToList();

                return Json(new { suggestions = allSuggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting search suggestions for term: {Term}", term);
                return Json(new { suggestions = new string[0] });
            }
        }
    }

    public class SearchResultsViewModel
    {
        public List<Invention> Inventions { get; set; } = new List<Invention>();
        public int TotalInventions { get; set; }
        public int TotalUsers { get; set; }
    }
}