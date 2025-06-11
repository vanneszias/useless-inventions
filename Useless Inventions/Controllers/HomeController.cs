using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Useless_Inventions.Models;

namespace Useless_Inventions.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // If user is not authenticated, redirect to Explore page
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Trends", "Explore");
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
