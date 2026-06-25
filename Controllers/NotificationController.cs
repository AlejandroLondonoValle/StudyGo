// ============================================================================
// StudyGo · Controllers/NotificationController.cs
// ============================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyGo.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace StudyGo.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdString, out Guid userId) ? userId : Guid.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Challenge();

            var viewModel = await _notificationService.GetNotificationsAsync(userId);
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var dropdownModel = await _notificationService.GetDropdownAsync(userId);
            return Json(new { unreadCount = dropdownModel.UnreadCount, items = dropdownModel.Items });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (userId != Guid.Empty)
            {
                await _notificationService.MarkAllAsReadAsync(userId);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}