using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.Controllers;

public class InventionsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public InventionsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Inventions
    public async Task<IActionResult> Index()
    {
        IList<Invention> inventions = await _context.Inventions
            .Include(i => i.User)
            .Include(i => i.Likes)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        return View(inventions);
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

    // GET: Inventions/Create
    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Inventions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Create([Bind("Title,Description,ImageUrl")] Invention invention)
    {
        if (ModelState.IsValid)
        {
            invention.UserId = _userManager.GetUserId(User)!;
            invention.CreatedAt = DateTime.UtcNow;
            
            _context.Add(invention);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(invention);
    }

    // POST: Inventions/Comment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddComment(int inventionId, string content)
    {
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
        Comment? comment = await _context.Comments.FindAsync(commentId);
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

        Invention? invention = await _context.Inventions.FindAsync(id);
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

        Invention? existingInvention = await _context.Inventions.FindAsync(id);
        if (existingInvention == null || existingInvention.UserId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                existingInvention.Title = invention.Title;
                existingInvention.Description = invention.Description;
                existingInvention.ImageUrl = invention.ImageUrl;
                existingInvention.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!InventionExists(invention.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
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

    private bool InventionExists(int id)
    {
        return _context.Inventions.Any(e => e.Id == id);
    }
}