using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Useless_Inventions.Data;
using Useless_Inventions.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Authorization;

namespace Useless_Inventions.Controllers;

public class ProfileController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Profile/{username}
    [HttpGet("/Profile/{username}")]
    public async Task<IActionResult> Index(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return NotFound();

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (user == null)
            return NotFound();

        var inventions = await _context.Inventions
            .Where(i => i.UserId == user.Id)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var currentUserId = _userManager.GetUserId(User);
        bool isCurrentUser = user.Id == currentUserId;
        bool isFollowing = false;
        bool showFollowButton = false;
        if (!isCurrentUser && currentUserId != null)
        {
            isFollowing = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == user.Id);
            showFollowButton = true;
        }

        var model = new UserProfileCardViewModel
        {
            UserName = user.UserName ?? "User",
            Email = string.Empty, // Not displayed for privacy
            AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? "/images/avatar-placeholder.png" : user.AvatarUrl,
            InventionCount = inventions.Count,
            IsCurrentUser = isCurrentUser,
            ShowFollowButton = showFollowButton,
            IsFollowing = isFollowing
        };

        ViewBag.Inventions = inventions;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(string username)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(username))
            return RedirectToAction("Index", new { username });

        var userToFollow = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (userToFollow == null || userToFollow.Id == currentUserId)
            return RedirectToAction("Index", new { username });

        bool alreadyFollowing = await _context.Follows.AnyAsync(f => f.FollowerId == currentUserId && f.FolloweeId == userToFollow.Id);
        if (!alreadyFollowing)
        {
            _context.Follows.Add(new Follow { FollowerId = currentUserId, FolloweeId = userToFollow.Id });
            await _context.SaveChangesAsync();

            // Create notification for the user being followed
            await NotificationsController.CreateNotificationAsync(
                _context,
                userToFollow.Id,
                currentUserId,
                NotificationType.Follow,
                $"started following you"
            );
        }
        return RedirectToAction("Index", new { username });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(string username)
    {
        var currentUserId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(username))
            return RedirectToAction("Index", new { username });

        var userToUnfollow = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (userToUnfollow == null || userToUnfollow.Id == currentUserId)
            return RedirectToAction("Index", new { username });

        var follow = await _context.Follows.FirstOrDefaultAsync(f => f.FollowerId == currentUserId && f.FolloweeId == userToUnfollow.Id);
        if (follow != null)
        {
            _context.Follows.Remove(follow);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index", new { username });
    }

    [HttpGet("/Profile/Edit")]
    public async Task<IActionResult> Edit()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return RedirectToAction("Index", "Home");
        var model = new UserProfileCardViewModel {
            UserName = currentUser.UserName ?? "",
            Email = currentUser.Email ?? "",
            AvatarUrl = string.IsNullOrEmpty(currentUser.AvatarUrl) ? "/images/avatar-placeholder.png" : currentUser.AvatarUrl
        };
        return View(model);
    }

    [HttpPost("/Profile/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserProfileCardViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction("Index", "Home");
        user.UserName = model.UserName;
        user.Email = model.Email;
        if (!string.IsNullOrEmpty(model.AvatarUrl))
        {
            user.AvatarUrl = model.AvatarUrl;
        }
        await _userManager.UpdateAsync(user);
        return RedirectToAction("Index", new { username = model.UserName });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> UploadAvatar(IFormFile uploadedImage)
    {
        // THIS IS A VERY UNSAFE FUNCTION TO UPLOAD IMAGES TO THE SERVER BTW...
        if (uploadedImage == null || uploadedImage.Length == 0)
            return BadRequest("No file uploaded.");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploads/avatars");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"avatar_{User.Identity.Name}_{Guid.NewGuid().ToString("N")}{Path.GetExtension(uploadedImage.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await uploadedImage.CopyToAsync(stream);
        }
        var relativePath = $"/images/uploads/avatars/{fileName}";
        // Save relativePath to user's profile in DB
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            user.AvatarUrl = relativePath;
            await _userManager.UpdateAsync(user);
        }
        return Json(new { imageUrl = relativePath });
    }
} 