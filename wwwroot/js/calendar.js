// ============================================================================
// StudyGo · wwwroot/js/calendar.js
// ============================================================================
document.addEventListener('DOMContentLoaded', function () {
    var calendarEl = document.getElementById('calendar');

    if (!calendarEl) return;

    var calendar = new FullCalendar.Calendar(calendarEl, {
        initialView: 'dayGridMonth',
        themeSystem: 'standard',
        headerToolbar: {
            left: 'prev,next today',
            center: 'title',
            right: 'dayGridMonth,timeGridWeek,timeGridDay'
        },
        buttonText: {
            today: 'Hoy',
            month: 'Mes',
            week: 'Semana',
            day: 'Día'
        },
        locale: 'es',
        firstDay: 1,
        events: '/Calendar/GetEvents',
        eventClick: function (info) {
            const eventData = info.event.extendedProps;
            const msg = `Detalles: ${info.event.title}\nCurso ID: ${eventData.courseId}`;

            if (typeof showToast === "function") {
                showToast(msg, "info");
            }
        }
    });

    calendar.render();
});