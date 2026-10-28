using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Controllers;

[Authorize(Roles = "Member")]
public class AssessmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssessmentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Take(int courseId, AssessmentType type)
    {
        if (!Enum.IsDefined(type)) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == courseId)) return Forbid();

        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();
        if (type == AssessmentType.PostTest && !await AllLessonsCompletedAsync(courseId, user.Id))
        {
            TempData["Error"] = "เรียนบทเรียนทั้งหมดให้ครบก่อนเริ่ม Post-test นะครับ";
            return RedirectToAction("MyCourses", "Learning");
        }

        var questions = await _context.AssessmentQuestions
            .Where(q => q.CourseId == courseId && q.Type == type)
            .Include(q => q.Options.OrderBy(o => o.OrderNumber))
            .OrderBy(q => q.OrderNumber)
            .ToListAsync();
        if (questions.Count == 0)
        {
            TempData["Error"] = "ข้อสอบยังไม่มีคำถาม กรุณาติดต่อผู้ดูแลระบบ";
            return RedirectToAction("MyCourses", "Learning");
        }

        // Shuffle displayed options for every attempt; correctness remains tied to option IDs.
        foreach (var question in questions)
        {
            var shuffledOptions = question.Options.OrderBy(_ => Random.Shared.Next()).ToList();
            for (var index = 0; index < shuffledOptions.Count; index++)
            {
                shuffledOptions[index].OrderNumber = index + 1;
            }
            question.Options = shuffledOptions;
        }

        return View(new AssessmentViewModel { Course = course, Type = type, Questions = questions });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitAssessmentViewModel model)
    {
        if (!Enum.IsDefined(model.Type)) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == model.CourseId)) return Forbid();

        var course = await _context.Courses.FindAsync(model.CourseId);
        if (course == null) return NotFound();
        if (model.Type == AssessmentType.PostTest && !await AllLessonsCompletedAsync(model.CourseId, user.Id)) return Forbid();

        var questions = await _context.AssessmentQuestions
            .Where(q => q.CourseId == model.CourseId && q.Type == model.Type)
            .Include(q => q.Options)
            .OrderBy(q => q.OrderNumber)
            .ToListAsync();
        if (questions.Count == 0) return NotFound();

        var correctCount = 0;
        var attempt = new AssessmentAttempt
        {
            CourseId = model.CourseId,
            UserId = user.Id,
            Type = model.Type,
            TotalQuestions = questions.Count,
            CompletedAt = DateTime.UtcNow
        };

        foreach (var question in questions)
        {
            model.SelectedOptions.TryGetValue(question.Id, out var selectedOptionId);
            var selected = question.Options.FirstOrDefault(o => o.Id == selectedOptionId);
            // Ignore forged option IDs which do not belong to this question.
            var isCorrect = selected?.IsCorrect == true;
            if (isCorrect) correctCount++;
            attempt.Answers.Add(new AssessmentAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = selected?.Id,
                IsCorrect = isCorrect
            });
        }

        attempt.Score = (int)Math.Round(correctCount * 100.0 / questions.Count, MidpointRounding.AwayFromZero);
        // Pre-test is diagnostic only; the passing score applies to Post-test and Certificate eligibility.
        attempt.Passed = model.Type == AssessmentType.PreTest || attempt.Score >= course.CertificatePassingScore;
        _context.AssessmentAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        if (model.Type == AssessmentType.PostTest && attempt.Passed && await AllLessonsCompletedAsync(model.CourseId, user.Id))
        {
            var certificate = await _context.CourseCertificates.FirstOrDefaultAsync(c => c.CourseId == model.CourseId && c.UserId == user.Id);
            if (certificate == null)
            {
                certificate = new CourseCertificate
                {
                    CourseId = model.CourseId,
                    UserId = user.Id,
                    CertificateNumber = $"DU-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                    IssuedAt = DateTime.UtcNow
                };
                _context.CourseCertificates.Add(certificate);
                await _context.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Result), new { id = attempt.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Result(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var attempt = await _context.AssessmentAttempts
            .Include(a => a.Course)
            .Include(a => a.Answers).ThenInclude(a => a.Question)
            .Include(a => a.Answers).ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
        if (attempt == null || attempt.Course == null) return NotFound();

        var certificate = attempt.Type == AssessmentType.PostTest && attempt.Passed
            ? await _context.CourseCertificates.FirstOrDefaultAsync(c => c.CourseId == attempt.CourseId && c.UserId == user.Id)
            : null;
        return View(new AssessmentResultViewModel { Course = attempt.Course, Attempt = attempt, Certificate = certificate });
    }

    [HttpGet]
    public async Task<IActionResult> Certificate(int courseId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var certificate = await _context.CourseCertificates
            .Include(c => c.Course)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.CourseId == courseId && c.UserId == user.Id);
        if (certificate == null || certificate.Course == null || certificate.User == null) return NotFound();
        return View(certificate);
    }

    private async Task<bool> AllLessonsCompletedAsync(int courseId, string userId)
    {
        var lessonIds = await _context.Lessons.Where(l => l.CourseId == courseId).Select(l => l.Id).ToListAsync();
        if (lessonIds.Count == 0) return false;
        var completedCount = await _context.LessonCompletions
            .CountAsync(c => c.UserId == userId && lessonIds.Contains(c.LessonId));
        return completedCount == lessonIds.Count;
    }
}
