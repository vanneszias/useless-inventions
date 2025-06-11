using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Useless_Inventions.Data;
using Useless_Inventions.Models;

namespace Useless_Inventions.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string filter = "all")
        {
            ViewData["Title"] = "Notifications";
            ViewBag.Filter = filter;

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var notificationsQuery = _context.Notifications
                    .Include(n => n.FromUser)
                    .Include(n => n.Invention)
                    .Where(n => n.ToUserId == userId);

                // Apply filter
                switch (filter.ToLower())
                {
                    case "likes":
                        notificationsQuery = notificationsQuery.Where(n => n.Type == NotificationType.Like);
                        break;
                    case "comments":
                        notificationsQuery = notificationsQuery.Where(n => n.Type == NotificationType.Comment);
                        break;
                    case "follows":
                        notificationsQuery = notificationsQuery.Where(n => n.Type == NotificationType.Follow);
                        break;
                    case "unread":
                        notificationsQuery = notificationsQuery.Where(n => !n.IsRead);
                        break;
                    default:
                        // Show all notifications
                        break;
                }

                var notifications = await notificationsQuery
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(50)
                    .ToListAsync();

                // Mark notifications as read when viewed
                var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
                if (unreadNotifications.Any())
                {
                    foreach (var notification in unreadNotifications)
                    {
                        notification.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }

                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                ViewBag.ErrorMessage = "Unable to load notifications at this time.";
                return View(new List<Notification>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.ToUserId == userId);

                if (notification != null)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Notification not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read", id);
                return Json(new { success = false, message = "Failed to mark notification as read." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.ToUserId == userId && !n.IsRead)
                    .ToListAsync();

                if (unreadNotifications.Any())
                {
                    foreach (var notification in unreadNotifications)
                    {
                        notification.IsRead = true;
                    }
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "All notifications marked as read." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { success = false, message = "Failed to mark all notifications as read." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == id && n.ToUserId == userId);

                if (notification != null)
                {
                    _context.Notifications.Remove(notification);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Notification not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {NotificationId}", id);
                return Json(new { success = false, message = "Failed to delete notification." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.Notifications
                    .Where(n => n.ToUserId == userId && !n.IsRead)
                    .CountAsync();

                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread notification count for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { notifications = new List<object>() });
                }

                var recentNotifications = await _context.Notifications
                    .Include(n => n.FromUser)
                    .Include(n => n.Invention)
                    .Where(n => n.ToUserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .Select(n => new
                    {
                        id = n.Id,
                        type = n.Type.ToString(),
                        message = n.Message,
                        fromUser = n.FromUser != null ? new
                        {
                            id = n.FromUser.Id,
                            userName = n.FromUser.UserName,
                            avatarUrl = n.FromUser.AvatarUrl
                        } : null,
                        invention = n.Invention != null ? new
                        {
                            id = n.Invention.Id,
                            title = n.Invention.Title
                        } : null,
                        createdAt = n.CreatedAt,
                        isRead = n.IsRead
                    })
                    .ToListAsync();

                return Json(new { notifications = recentNotifications });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent notifications for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Json(new { notifications = new List<object>() });
            }
        }

        // Helper method to create notifications (would be called from other controllers)
        public static async Task CreateNotificationAsync(
            ApplicationDbContext context,
            string toUserId,
            string? fromUserId,
            NotificationType type,
            string message,
            int? inventionId = null)
        {
            try
            {
                // Don't create notification if user is notifying themselves
                if (toUserId == fromUserId)
                    return;

                var notification = new Notification
                {
                    ToUserId = toUserId,
                    FromUserId = fromUserId,
                    Type = type,
                    Message = message,
                    InventionId = inventionId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Log error but don't throw - notifications shouldn't break main functionality
                // Logger would need to be passed in or injected differently for a static method
            }
        }
    }
}