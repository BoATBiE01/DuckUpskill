using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Controllers;

[Authorize]
public class LearningController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LearningController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> MyCourses()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var enrollments = await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c!.Lessons)
            .Where(e => e.UserId == user.Id)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var completedLessonIds = await _context.LessonCompletions
            .Where(lc => lc.UserId == user.Id)
            .Select(lc => lc.LessonId)
            .ToListAsync();

        var completedSet = completedLessonIds.ToHashSet();

        var enrolledItems = new List<EnrolledCourseItem>();

        foreach (var enrollment in enrollments)
        {
            if (enrollment.Course == null) continue;

            var lessons = enrollment.Course.Lessons.OrderBy(l => l.OrderNumber).ToList();
            var total = lessons.Count;
            var completedCount = lessons.Count(l => completedSet.Contains(l.Id));
            var progress = total == 0 ? 0 : (int)Math.Round((double)completedCount / total * 100);

            var nextLesson = lessons.FirstOrDefault(l => !completedSet.Contains(l.Id)) ?? lessons.FirstOrDefault();
            var completedCourse = total > 0 && completedCount == total;
            var hasPreTest = await _context.AssessmentAttempts.AnyAsync(a => a.UserId == user.Id && a.CourseId == enrollment.CourseId && a.Type == AssessmentType.PreTest);
            var certificate = completedCourse
                ? await _context.CourseCertificates.FirstOrDefaultAsync(c => c.UserId == user.Id && c.CourseId == enrollment.CourseId)
                : null;

            enrolledItems.Add(new EnrolledCourseItem
            {
                Enrollment = enrollment,
                TotalLessons = total,
                CompletedLessons = completedCount,
                ProgressPercent = progress,
                NextLesson = nextLesson,
                IsCourseCompleted = completedCourse,
                HasPreTest = hasPreTest,
                Certificate = certificate
            });
        }

        var orders = await _context.Orders
            .Include(o => o.Course)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        var model = new MyLearningViewModel
        {
            User = user,
            EnrolledCourses = enrolledItems,
            Orders = orders
        };

        return View(model);
    }
}
