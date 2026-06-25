// ============================================================================
// StudyGo · Services/CalendarService.cs
// ============================================================================
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.ViewModels; // Importante para reconocer FullCalendarEventViewModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace StudyGo.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly AppDbContext _context;

        public CalendarService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<FullCalendarEventViewModel>> GetEventsAsync(Guid userId, DateTime start, DateTime end)
        {
            // Consulta LINQ directa contra la tabla dbo.CalendarEvents usando EF Core
            var events = await _context.CalendarEvents
                .Where(e => e.StartsAt >= start && e.EndsAt <= end)
                .Select(e => new FullCalendarEventViewModel
                {
                    id = e.Id.ToString(),
                    title = e.Title,
                    start = e.StartsAt.ToString("yyyy-MM-ddTHH:mm:ss"), // Formato ISO estricto para FullCalendar JS
                    end = e.EndsAt.ToString("yyyy-MM-ddTHH:mm:ss"),
                    className = "event-purple", // Token de acento para la paleta oscura de StudyGo
                    extendedProps = new { courseId = e.CourseId.ToString() }
                })
                .ToListAsync();

            return events;
        }
    }
}