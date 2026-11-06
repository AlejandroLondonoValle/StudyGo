// ============================================================================
// StudyGo · Controllers/QuizController.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudyGo.Data;
using StudyGo.Enums;
using StudyGo.Models;
using StudyGo.Services;
using StudyGo.ViewModels;

namespace StudyGo.Controllers
{
    [Authorize(Roles = "Docente,Administrador")]
    public class QuizController : Controller
    {
        private readonly IQuizService _quizService;
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService; // Inyectado para Jaison

        public QuizController(IQuizService quizService, AppDbContext context, INotificationService notificationService)
        {
            _quizService = quizService;
            _context = context;
            _notificationService = notificationService; // Asignado para Jaison
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim);
        }

        private bool IsAdmin() => User.IsInRole("Administrador");

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var quizzes = await _quizService.GetQuizzesForUserAsync(userId, IsAdmin());

            var viewModel = new QuizDashboardViewModel
            {
                TotalQuizzes = quizzes.Count(),
                ActiveQuizzes = quizzes.Count(q => q.State == ActivityState.Publicado),
                PendingAttempts = quizzes.Sum(q => q.QuizAttempts.Count),
                CourseGroups = quizzes
                    .GroupBy(q => new { q.CourseId, q.Course.Name })
                    .Select(g => new CourseQuizGroup
                    {
                        CourseId = g.Key.CourseId,
                        CourseName = g.Key.Name,
                        Quizzes = g.Select(q => new QuizSummaryItem
                        {
                            Id = q.Id,
                            Title = q.Title,
                            StateName = q.State.ToString(),
                            TimeLimitMinutes = q.TimeLimitMinutes,
                            DueDate = q.DueDate,
                            OpenDate = q.OpenDate,
                            MaxAttempts = q.MaxAttempts,
                            TotalAttempts = q.QuizAttempts.Count,
                            QuestionCount = q.Questions.Count
                        }).ToList()
                    }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            var userId = GetCurrentUserId();
            var courses = await _quizService.GetAvailableCoursesAsync(userId, IsAdmin());

            var viewModel = new QuizViewModel
            {
                AvailableCourses = courses,
                State = ActivityState.Borrador,
                TimeLimitMinutes = 30,
                MaxAttempts = 1,
                Questions = new List<QuizQuestionViewModel>()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QuizViewModel model)
        {
            var userId = GetCurrentUserId();

            ModelState.Remove("Id");
            ModelState.Remove("CourseName");
            ModelState.Remove("AvailableCourses");

            if (model.CourseId == Guid.Empty)
            {
                ModelState.AddModelError("CourseId", "Debes seleccionar un curso.");
            }
            else
            {
                var course = await _context.Courses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == model.CourseId);

                if (course == null)
                    ModelState.AddModelError("CourseId", "El curso seleccionado no existe.");
                else if (!IsAdmin() && course.TeacherId != userId)
                    ModelState.AddModelError("CourseId", "No tienes permiso para crear quizzes en este curso.");
            }

            model.AvailableCourses = await _quizService.GetAvailableCoursesAsync(userId, IsAdmin());

            if (!ValidateQuizQuestions(model)) return View(model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join(" ", errors);
                return View(model);
            }

            // Guardar el quiz usando la capa de servicios existente de Negro
            var savedQuizId = await _quizService.CreateQuizAsync(model);

            // ============================================================================
            // INTEGRACIÓN DE COMUNICACIÓN (JAISON) - AL FINAL DEL CONTROLADOR
            // ============================================================================
            var quiz = await _context.Quizzes.FindAsync(savedQuizId);
            if (quiz != null)
            {
                if (quiz.OpenDate.HasValue && quiz.DueDate.HasValue)
                {
                    var calendarEvent = new CalendarEvent
                    {
                        Id = Guid.NewGuid(),
                        CourseId = quiz.CourseId,
                        Title = $"Quiz: {quiz.Title}",
                        StartsAt = quiz.OpenDate.Value,
                        EndsAt = quiz.DueDate.Value
                    };
                    _context.CalendarEvents.Add(calendarEvent);
                    await _context.SaveChangesAsync();
                }

                var enrolledStudents = await _context.Enrollments
                    .Where(e => e.CourseId == quiz.CourseId)
                    .Select(e => e.StudentId)
                    .ToListAsync();

                foreach (var studentId in enrolledStudents)
                {
                    await _notificationService.CreateNotificationAsync(new Notification
                    {
                        Id = Guid.NewGuid(),
                        UserId = studentId,
                        Type = "warn",
                        Message = $"Nuevo Quiz publicado: {quiz.Title}",
                        Link = $"/StudentQuiz/Index",
                        IsRead = false,
                        CreatedAt = DateTime.Now
                    });
                }
            }
            // ============================================================================

            TempData["SuccessMessage"] = "Quiz creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = GetCurrentUserId();

            if (!await _quizService.CanUserManageQuizAsync(id, userId, IsAdmin()))
                return Forbid();

            var quiz = await _quizService.GetQuizByIdAsync(id);
            if (quiz == null) return NotFound();

            var courses = await _quizService.GetAvailableCoursesAsync(userId, IsAdmin());

            var viewModel = new QuizViewModel
            {
                Id = quiz.Id,
                CourseId = quiz.CourseId,
                Title = quiz.Title,
                Description = quiz.Description,
                State = quiz.State,
                DueDate = quiz.DueDate,
                SelectionMode = quiz.SelectionMode,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                OpenDate = quiz.OpenDate,
                MaxAttempts = quiz.MaxAttempts,
                AvailableCourses = courses,
                Questions = quiz.Questions
                    .OrderBy(q => q.Order)
                    .Select(q => new QuizQuestionViewModel
                    {
                        Id = q.Id,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Order = q.Order,
                        Options = q.Options.Select(o => new QuizOptionViewModel
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            IsCorrect = o.IsCorrect
                        }).ToList()
                    }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, QuizViewModel model)
        {
            var userId = GetCurrentUserId();

            if (!await _quizService.CanUserManageQuizAsync(id, userId, IsAdmin()))
                return Forbid();

            ModelState.Remove("CourseName");
            ModelState.Remove("AvailableCourses");

            model.AvailableCourses = await _quizService.GetAvailableCoursesAsync(userId, IsAdmin());

            if (!ValidateQuizQuestions(model)) return View(model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                TempData["ErrorMessage"] = string.Join(" ", errors);
                model.Id = id;
                return View(model);
            }

            await _quizService.UpdateQuizAsync(id, model);
            TempData["SuccessMessage"] = "Quiz actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();

            if (!await _quizService.CanUserManageQuizAsync(id, userId, IsAdmin()))
                return Forbid();

            await _quizService.DeleteQuizAsync(id);

            TempData["SuccessMessage"] = "Quiz eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Stats(Guid id)
        {
            var userId = GetCurrentUserId();

            if (!await _quizService.CanUserManageQuizAsync(id, userId, IsAdmin()))
                return Forbid();

            await _quizService.GenerateMissingZeroAttemptsAsync(id);

            var (attempts, metadata) = await _quizService.GetQuizStatsAsync(id);

            ViewBag.QuizTitle = metadata["QuizTitle"];
            ViewBag.CourseName = metadata["CourseName"];
            ViewBag.TotalQuestions = metadata["TotalQuestions"];
            ViewBag.TimeLimitMinutes = metadata["TimeLimitMinutes"];
            ViewBag.StudentNames = metadata["StudentNames"];
            ViewBag.AverageScore = metadata["AverageScore"];

            return View(attempts);
        }

        private bool ValidateQuizQuestions(QuizViewModel model)
        {
            if (model.Questions == null || model.Questions.Count == 0)
            {
                ModelState.AddModelError("Questions", "Debe agregar al menos una pregunta al quiz.");
                return false;
            }

            for (int i = 0; i < model.Questions.Count; i++)
            {
                var q = model.Questions[i];
                if (string.IsNullOrWhiteSpace(q.QuestionText))
                    ModelState.AddModelError($"Questions[{i}].QuestionText", $"La pregunta {i + 1} no tiene texto.");

                if (q.Options == null || q.Options.Count < 2)
                    ModelState.AddModelError($"Questions[{i}].Options", $"La pregunta {i + 1} debe tener al menos 2 opciones.");
                else if (!q.Options.Any(o => o.IsCorrect))
                    ModelState.AddModelError($"Questions[{i}].Options", $"La pregunta {i + 1} debe tener al menos una opción correcta.");
            }

            return ModelState.IsValid;
        }
    }
}