// ============================================================================
// StudyGo · Services/ICalendarService.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StudyGo.ViewModels;

namespace StudyGo.Services
{
    public interface ICalendarService
    {
        Task<List<FullCalendarEventViewModel>> GetEventsAsync(Guid userId, DateTime start, DateTime end);
    }
}
