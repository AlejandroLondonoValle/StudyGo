// ============================================================================
// StudyGo · ViewModels/Notifications/NotificationViewModels.cs
// ============================================================================
using System;
using System.Collections.Generic;

namespace StudyGo.ViewModels.Notifications
{
    public class NotificationListViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationItemViewModel> Notifications { get; set; } = new();
    }

    public class NotificationItemViewModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Link { get; set; }
        public string TimeRelative { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NotificationDropdownViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationItemViewModel> Items { get; set; } = new();
        public bool HasMore { get; set; }
    }
}