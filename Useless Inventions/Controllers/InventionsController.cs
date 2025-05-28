using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using Useless_Inventions.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

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
        var invention = feedViewModel.NewInvention;
        // Set required user properties before validation
        invention.UserId = _userManager.GetUserId(User)!;
        invention.CreatedAt = DateTime.UtcNow;
        invention.User = await _userManager.GetUserAsync(User)!;

        // Clear and revalidate ModelState after setting required properties
        ModelState.Clear();
        if (TryValidateModel(invention))
        {
            _context.Add(invention);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Invalid data. Please check your input.");
        foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
        {
            Console.WriteLine(error.ErrorMessage);
        }
        // Repopulate posts for the feed
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
        feedViewModel.Posts = postModels;
        return View("Index", feedViewModel);
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

        Invention? invention = await _context.Inventions.FindAsync(inventionId);
        if (invention == null)
        {
            return NotFound();
        }

        Comment comment = new()
        {
            Content = content,
            InventionId = inventionId,
            UserId = _userManager.GetUserId(User)!,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
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