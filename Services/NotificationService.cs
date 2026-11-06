// ============================================================================
// StudyGo · Services/NotificationService.cs
// ============================================================================
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Hubs;
using StudyGo.Models;
using StudyGo.ViewModels.Notifications;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StudyGo.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<NotificationListViewModel> GetNotificationsAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return new NotificationListViewModel
            {
                UnreadCount = notifications.Count(n => !n.IsRead),
                Notifications = notifications.Select(n => new NotificationItemViewModel
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,
                    Link = n.Link ?? "/",
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TimeRelative = CalcularTiempoRelativo(n.CreatedAt)
                }).ToList()
            };
        }

        public async Task<NotificationDropdownViewModel> GetDropdownAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(6)
                .ToListAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            return new NotificationDropdownViewModel
            {
                UnreadCount = unreadCount,
                Items = notifications.Take(5).Select(n => new NotificationItemViewModel
                {
                    Id = n.Id,
                    Type = n.Type,
                    Message = n.Message,
                    Link = n.Link ?? "/",
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    TimeRelative = CalcularTiempoRelativo(n.CreatedAt)
                }).ToList(),
                HasMore = notifications.Count > 5
            };
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Any())
            {
                foreach (var notif in unread) notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == notification.UserId && !n.IsRead);

            // Envío SignalR mapeado al Id de usuario en texto
            await _hubContext.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotification", new
            {
                type = notification.Type,
                message = notification.Message,
                unreadCount = unreadCount
            });
        }

        private string CalcularTiempoRelativo(DateTime fecha)
        {
            var span = DateTime.Now - fecha;
            if (span.Days > 365) return $"Hace {span.Days / 365} años";
            if (span.Days > 30) return $"Hace {span.Days / 30} meses";
            if (span.Days > 0) return $"Hace {span.Days}d";
            if (span.Hours > 0) return $"Hace {span.Hours}h";
            if (span.Minutes > 0) return $"Hace {span.Minutes}m";
            return "Justo ahora";
        }
    }
}
