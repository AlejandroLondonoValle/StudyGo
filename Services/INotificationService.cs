// ============================================================================
// StudyGo · Services/INotificationService.cs
// ============================================================================
using System;
using System.Threading.Tasks;
using StudyGo.Models;
using StudyGo.ViewModels.Notifications;

namespace StudyGo.Services
{
    public interface INotificationService
    {
        Task<NotificationListViewModel> GetNotificationsAsync(Guid userId);
        Task<NotificationDropdownViewModel> GetDropdownAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
        Task CreateNotificationAsync(Notification notification);
    }
}