using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.Controllers
{
    [Authorize]
    public class CreateController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CreateController> _logger;

        public CreateController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<CreateController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Create New Invention";
            return View(new CreateInventionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CreateInventionViewModel model)
        {
            ViewData["Title"] = "Create New Invention";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    ModelState.AddModelError("", "You must be logged in to create an invention.");
                    return View(model);
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    ModelState.AddModelError("", "User not found.");
                    return View(model);
                }

                // Handle image upload if provided
                string? imageUrl = null;
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    imageUrl = await ProcessImageUpload(model.ImageFile);
                    if (imageUrl == null)
                    {
                        ModelState.AddModelError("ImageFile", "Failed to upload image. Please try again.");
                        return View(model);
                    }
                }

                // Create the invention
                var invention = new Invention
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    ImageUrl = imageUrl,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Inventions.Add(invention);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created invention {InventionId}: {Title}", 
                    userId, invention.Id, invention.Title);

                TempData["SuccessMessage"] = "Your invention has been created successfully!";
                return RedirectToAction("Details", "Inventions", new { id = invention.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invention for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ModelState.AddModelError("", "An error occurred while creating your invention. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Draft(int id)
        {
            ViewData["Title"] = "Edit Draft";

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var invention = await _context.Inventions
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

                if (invention == null)
                {
                    return NotFound();
                }

                var model = new CreateInventionViewModel
                {
                    Id = invention.Id,
                    Title = invention.Title,
                    Description = invention.Description,
                    ExistingImageUrl = invention.ImageUrl,
                    IsDraft = true
                };

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading draft {InventionId}", id);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Invention";

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var invention = await _context.Inventions
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

                if (invention == null)
                {
                    return NotFound();
                }

                var model = new CreateInventionViewModel
                {
                    Id = invention.Id,
                    Title = invention.Title,
                    Description = invention.Description,
                    ExistingImageUrl = invention.ImageUrl,
                    IsDraft = true
                };

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invention {InventionId}", id);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft(CreateInventionViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "You must be logged in." });
                }

                // Basic validation for draft
                if (string.IsNullOrWhiteSpace(model.Title) && string.IsNullOrWhiteSpace(model.Description))
                {
                    return Json(new { success = false, message = "Please add some content before saving." });
                }

                Invention invention;
                
                if (model.Id > 0)
                {
                    // Update existing draft
                    invention = await _context.Inventions
                        .FirstOrDefaultAsync(i => i.Id == model.Id && i.UserId == userId);
                    
                    if (invention == null)
                    {
                        return Json(new { success = false, message = "Draft not found." });
                    }

                    invention.Title = model.Title?.Trim() ?? "";
                    invention.Description = model.Description?.Trim() ?? "";
                    invention.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new draft
                    invention = new Invention
                    {
                        Title = model.Title?.Trim() ?? "",
                        Description = model.Description?.Trim() ?? "",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Inventions.Add(invention);
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Draft saved successfully!",
                    draftId = invention.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving draft");
                return Json(new { success = false, message = "Failed to save draft. Please try again." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Drafts()
        {
            ViewData["Title"] = "My Drafts";

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var drafts = await _context.Inventions
                    .Where(i => i.UserId == userId)
                    .OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                    .ToListAsync();

                return View(drafts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading drafts for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return View(new List<Invention>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDraft(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var draft = await _context.Inventions
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

                if (draft == null)
                {
                    return Json(new { success = false, message = "Draft not found." });
                }

                _context.Inventions.Remove(draft);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Draft deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting draft {DraftId}", id);
                return Json(new { success = false, message = "Failed to delete draft." });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var invention = await _context.Inventions
                    .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);

                if (invention == null)
                {
                    return NotFound();
                }

                _context.Inventions.Remove(invention);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invention {InventionId}", id);
                return StatusCode(500);
            }
        }

        private async Task<string?> ProcessImageUpload(IFormFile imageFile)
        {
            try
            {
                // Validate file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    return null;
                }

                if (imageFile.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    return null;
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "inventions");
                
                // Ensure directory exists
                Directory.CreateDirectory(uploadsFolder);
                
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Return relative URL
                return $"/uploads/inventions/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image upload");
                return null;
            }
        }
    }

    public class CreateInventionViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 100 characters")]
        [Display(Name = "Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [MinLength(10, ErrorMessage = "Description must be at least 10 characters long")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Image")]
        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }

        public bool IsDraft { get; set; }
    }
}