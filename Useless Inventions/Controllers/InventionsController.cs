using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using Useless_Inventions.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace Useless_Inventions.Controllers;

public class InventionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public InventionsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Inventions
    public async Task<IActionResult> Index()
    {
        var currentUserId = _userManager.GetUserId(User);
        var inventions = await _context.Inventions
            .Include(i => i.User)
            .Include(i => i.Likes)
            .Include(i => i.Comments)
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
        var feedViewModel = new FeedViewModel {
            Posts = postModels,
            NewInvention = new Invention()
        };
        return View(feedViewModel);
    }

    // GET: Inventions/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Invention? invention = await _context.Inventions
            .Include(i => i.User)
            .Include(i => i.Comments)
                .ThenInclude(c => c.User)
            .Include(i => i.Likes)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invention == null)
        {
            return NotFound();
        }

        return View(invention);
    }

    // POST: Inventions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Create(FeedViewModel feedViewModel)
    {
        try
        {
            var invention = feedViewModel.NewInvention;
            if (invention == null)
            {
                TempData["ErrorMessages"] = new List<string> { "Invalid form data. Please try again." };
                return RedirectToAction("Index", "Home");
            }
            
            // Set required user properties before validation
            invention.UserId = _userManager.GetUserId(User)!;
            invention.CreatedAt = DateTime.UtcNow;
            invention.User = await _userManager.GetUserAsync(User)!;

            // Manual validation to ensure we catch the errors properly
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(invention.Title))
            {
                errors.Add("Title is required");
            }
            else if (invention.Title.Trim().Length < 3)
            {
                errors.Add("Title must be at least 3 characters long");
            }
            else if (invention.Title.Length > 100)
            {
                errors.Add("Title must be no more than 100 characters long");
            }

            if (string.IsNullOrWhiteSpace(invention.Description))
            {
                errors.Add("Description is required");
            }
            else if (invention.Description.Trim().Length < 10)
            {
                errors.Add("Description must be at least 10 characters long");
            }

            if (errors.Any())
            {
                TempData["ErrorMessages"] = errors;
                TempData["FormData"] = new Dictionary<string, string>
                {
                    ["Title"] = invention.Title ?? "",
                    ["Description"] = invention.Description ?? "",
                    ["ImageUrl"] = invention.ImageUrl ?? ""
                };
                return RedirectToAction("Index", "Home");
            }

            // Trim the input
            invention.Title = invention.Title.Trim();
            invention.Description = invention.Description.Trim();

            _context.Add(invention);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Your invention has been shared successfully!";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            // Log the error and provide user feedback
            var errorMessage = "An error occurred while creating your invention. Please try again.";
            TempData["ErrorMessages"] = new List<string> { errorMessage };
            
            // Preserve form data
            var invention = feedViewModel?.NewInvention;
            TempData["FormData"] = new Dictionary<string, string>
            {
                ["Title"] = invention?.Title ?? "",
                ["Description"] = invention?.Description ?? "",
                ["ImageUrl"] = invention?.ImageUrl ?? ""
            };
            
            return RedirectToAction("Index", "Home");
        }
    }

    // POST: Inventions/Comment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddComment(int inventionId, string content)
    {
        content = content?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
        {
            ModelState.AddModelError(nameof(content),
                "Comment text is required and must be ≤ 1000 characters.");
            return RedirectToAction(nameof(Details), new { id = inventionId });
        }

        Invention? invention = await _context.Inventions
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == inventionId);
        if (invention == null)
        {
            return NotFound();
        }

        string currentUserId = _userManager.GetUserId(User)!;
        Comment comment = new()
        {
            Content = content,
            InventionId = inventionId,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Create notification for the invention owner (if not commenting on own invention)
        if (invention.UserId != currentUserId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            await NotificationsController.CreateNotificationAsync(
                _context,
                invention.UserId,
                currentUserId,
                NotificationType.Comment,
                $"commented on your invention",
                inventionId
            );
        }

        return RedirectToAction(nameof(Details), new { id = inventionId });
    }

    // POST: Inventions/DeleteComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int commentId, int inventionId)
    {
        Comment? comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.InventionId == inventionId);
        if (comment == null || comment.UserId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = inventionId });
    }

    // POST: Inventions/ToggleLike
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> ToggleLike(int id)
    {
        Invention? invention = await _context.Inventions
            .Include(i => i.Likes)
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invention == null)
        {
            return NotFound();
        }

        string userId = _userManager.GetUserId(User)!;
        Like? existingLike = invention.Likes.FirstOrDefault(l => l.UserId == userId);

        if (existingLike != null)
        {
            _context.Likes.Remove(existingLike);
        }
        else
        {
            invention.Likes.Add(new Like
            {
                InventionId = id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });

            // Create notification for the invention owner (if not liking own invention)
            if (invention.UserId != userId)
            {
                await NotificationsController.CreateNotificationAsync(
                    _context,
                    invention.UserId,
                    userId,
                    NotificationType.Like,
                    $"liked your invention",
                    id
                );
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Inventions/Edit/5
    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Invention? invention = await _context.Inventions
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (invention == null || invention.UserId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        return View(invention);
    }

    // POST: Inventions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,ImageUrl")] Invention invention)
    {
        if (id != invention.Id)
        {
            return NotFound();
        }

        Invention? existingInvention = await _context.Inventions
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (existingInvention == null || existingInvention.UserId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        // Preserve existing values that shouldn't change
        invention.UserId = existingInvention.UserId;
        invention.User = existingInvention.User;
        invention.CreatedAt = existingInvention.CreatedAt;
        invention.UpdatedAt = DateTime.UtcNow;

        // Clear and revalidate ModelState after setting required properties
        ModelState.Clear();
        if (TryValidateModel(invention))
        {
            try
            {
                _context.Entry(existingInvention).CurrentValues.SetValues(invention);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventionExists(invention.Id))
                {
                    return NotFound();
                }
                throw;
            }
        }

        ModelState.AddModelError(string.Empty, "Invalid data. Please check your input.");
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine(error.ErrorMessage);
        }
        return View(invention);
    }

    // POST: Inventions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        Invention? invention = await _context.Inventions.FindAsync(id);
        if (invention == null || invention.UserId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        _context.Inventions.Remove(invention);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UploadInventionImage(IFormFile uploadedImage, int inventionId = 0)
    {
        try
        {
            if (uploadedImage == null || uploadedImage.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploads/inventions");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"invention_{User.Identity.Name}_{Guid.NewGuid():N}{Path.GetExtension(uploadedImage.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await uploadedImage.CopyToAsync(stream);
            }
            var relativePath = $"/images/uploads/inventions/{fileName}";

            // Only update invention if inventionId is provided and not zero
            if (inventionId != 0)
            {
                var invention = await _context.Inventions.FindAsync(inventionId);
                if (invention != null)
                {
                    invention.ImageUrl = relativePath;
                    await _context.SaveChangesAsync();
                }
            }
            return Json(new { imageUrl = relativePath });
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Image upload failed.");
        }
    }

    private bool InventionExists(int id)
    {
        return _context.Inventions.Any(e => e.Id == id);
    }
}