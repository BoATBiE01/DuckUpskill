using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Controllers;

public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CoursesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? category, string? priceType)
    {
        var query = _context.Courses.Include(c => c.Lessons).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(c => c.Title.Contains(searchTerm) || c.ShortDescription.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(c => c.Category == category);
        }

        if (priceType == "free")
        {
            query = query.Where(c => c.IsFree);
        }
        else if (priceType == "paid")
        {
            query = query.Where(c => !c.IsFree);
        }

        var model = new CourseCatalogViewModel
        {
            Courses = await query.OrderByDescending(c => c.IsFeatured).ThenByDescending(c => c.CreatedAt).ToListAsync(),
            SearchTerm = searchTerm,
            Category = category,
            PriceType = priceType,
            Categories = await _context.Courses.Select(c => c.Category).Distinct().OrderBy(c => c).ToListAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        var course = await _context.Courses
            .Include(c => c.Lessons.OrderBy(l => l.OrderNumber))
            .FirstOrDefaultAsync(c => c.Slug == id);

        if (course == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var isEnrolled = user != null && await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == course.Id);
        var completedLessonIds = user == null
            ? new List<int>()
            : await _context.LessonCompletions
                .Where(lc => lc.UserId == user.Id && lc.Lesson != null && lc.Lesson.CourseId == course.Id)
                .Select(lc => lc.LessonId)
                .ToListAsync();

        var model = new CourseDetailsViewModel
        {
            Course = course,
            IsEnrolled = isEnrolled,
            IsAuthenticated = user != null,
            IsAdmin = User.IsInRole("Admin"),
            EnrolledCount = await _context.Enrollments.CountAsync(e => e.CourseId == course.Id),
            CompletedCount = completedLessonIds.Count,
            ProgressPercent = course.Lessons.Count == 0 ? 0 : (int)Math.Round(completedLessonIds.Count * 100.0 / course.Lessons.Count)
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        if (!course.IsFree)
        {
            return RedirectToAction("Checkout", "Orders", new { courseId });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var isEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == courseId);
        if (!isEnrolled)
        {
            _context.Enrollments.Add(new Enrollment { UserId = user.Id, CourseId = courseId });
            await _context.SaveChangesAsync();
            TempData["Success"] = "สมัครเรียนสำเร็จแล้ว! พร้อมเริ่มอัปสกิลไปด้วยกัน 🦆";
        }

        return RedirectToAction("Learn", new { id = course.Slug });
    }

    [Authorize]
    public async Task<IActionResult> Learn(string id, int? lessonId)
    {
        var course = await _context.Courses
            .Include(c => c.Lessons.OrderBy(l => l.OrderNumber))
            .FirstOrDefaultAsync(c => c.Slug == id);
        if (course == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var isEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == course.Id);
        if (!isEnrolled)
        {
            TempData["Error"] = "กรุณาสมัครเรียนก่อนเข้าสู่บทเรียน";
            return RedirectToAction("Details", new { id });
        }

        var lessons = course.Lessons.OrderBy(l => l.OrderNumber).ToList();
        var currentLesson = lessonId.HasValue
            ? lessons.FirstOrDefault(l => l.Id == lessonId.Value)
            : lessons.FirstOrDefault();
        if (currentLesson == null)
        {
            return NotFound();
        }

        var completedLessonIds = await _context.LessonCompletions
            .Where(lc => lc.UserId == user.Id && lc.Lesson != null && lc.Lesson.CourseId == course.Id)
            .Select(lc => lc.LessonId)
            .ToListAsync();
        var watchedLessonIds = await _context.LessonVideoWatches
            .Where(w => w.UserId == user.Id && w.Lesson != null && w.Lesson.CourseId == course.Id)
            .Select(w => w.LessonId)
            .ToListAsync();
        var currentIndex = lessons.FindIndex(l => l.Id == currentLesson.Id);

        var latestPreTest = await _context.AssessmentAttempts
            .Where(a => a.UserId == user.Id && a.CourseId == course.Id && a.Type == AssessmentType.PreTest)
            .OrderByDescending(a => a.CompletedAt)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        var latestPostTest = await _context.AssessmentAttempts
            .Where(a => a.UserId == user.Id && a.CourseId == course.Id && a.Type == AssessmentType.PostTest)
            .OrderByDescending(a => a.CompletedAt)
            .Select(a => (int?)a.Id)
            .FirstOrDefaultAsync();
        var hasPreTest = await _context.AssessmentQuestions.AnyAsync(q => q.CourseId == course.Id && q.Type == AssessmentType.PreTest);
        var hasPostTest = await _context.AssessmentQuestions.AnyAsync(q => q.CourseId == course.Id && q.Type == AssessmentType.PostTest);
        var certificateId = await _context.CourseCertificates
            .Where(c => c.UserId == user.Id && c.CourseId == course.Id)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        var model = new CoursePlayerViewModel
        {
            Course = course,
            CurrentLesson = currentLesson,
            Lessons = lessons,
            CompletedLessonIds = completedLessonIds.ToHashSet(),
            WatchedLessonIds = watchedLessonIds.ToHashSet(),
            CurrentVideoRequiresWatch = !string.IsNullOrWhiteSpace(currentLesson.VideoUrl),
            CurrentVideoWatched = watchedLessonIds.Contains(currentLesson.Id),
            IsEnrolled = true,
            ProgressPercent = lessons.Count == 0 ? 0 : (int)Math.Round(completedLessonIds.Count * 100.0 / lessons.Count),
            PreviousLesson = currentIndex > 0 ? lessons[currentIndex - 1] : null,
            NextLesson = currentIndex >= 0 && currentIndex < lessons.Count - 1 ? lessons[currentIndex + 1] : null,
            HasPreTest = hasPreTest,
            HasPostTest = hasPostTest,
            CanTakePostTest = lessons.Count > 0 && lessons.All(l => completedLessonIds.Contains(l.Id)),
            LatestPreTestAttemptId = latestPreTest,
            LatestPostTestAttemptId = latestPostTest,
            CertificateId = certificateId
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkVideoWatched(int lessonId)
    {
        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null || lesson.Course == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == lesson.CourseId)) return Forbid();
        if (string.IsNullOrWhiteSpace(lesson.VideoUrl)) return BadRequest();

        var alreadyWatched = await _context.LessonVideoWatches
            .AnyAsync(w => w.UserId == user.Id && w.LessonId == lessonId);
        if (!alreadyWatched)
        {
            _context.LessonVideoWatches.Add(new LessonVideoWatch
            {
                UserId = user.Id,
                LessonId = lessonId,
                WatchedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteLesson(int lessonId)
    {
        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == lessonId);
        if (lesson == null || lesson.Course == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var isEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == lesson.CourseId);
        if (!isEnrolled)
        {
            return Forbid();
        }

        if (!string.IsNullOrWhiteSpace(lesson.VideoUrl))
        {
            var videoWasWatched = await _context.LessonVideoWatches.AnyAsync(w => w.UserId == user.Id && w.LessonId == lessonId);
            if (!videoWasWatched)
            {
                TempData["Error"] = "กรุณาดูวิดีโอของบทเรียนนี้ให้จบก่อนทำเครื่องหมายว่าเรียนจบแล้ว";
                return RedirectToAction("Learn", new { id = lesson.Course.Slug, lessonId });
            }
        }

        var completed = await _context.LessonCompletions.AnyAsync(lc => lc.UserId == user.Id && lc.LessonId == lessonId);
        if (!completed)
        {
            _context.LessonCompletions.Add(new LessonCompletion { UserId = user.Id, LessonId = lessonId });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Learn", new { id = lesson.Course.Slug, lessonId });
    }
}
