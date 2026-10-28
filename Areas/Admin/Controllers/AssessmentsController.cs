using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AssessmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AssessmentsController(ApplicationDbContext context) => _context = context;

    public async Task<IActionResult> Index(int courseId, AssessmentType? type)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();
        var query = _context.AssessmentQuestions.Where(q => q.CourseId == courseId).Include(q => q.Options).AsQueryable();
        if (type.HasValue) query = query.Where(q => q.Type == type.Value);
        ViewBag.Course = course;
        ViewBag.Type = type;
        return View(await query.OrderBy(q => q.Type).ThenBy(q => q.OrderNumber).ToListAsync());
    }

    public async Task<IActionResult> Create(int courseId, AssessmentType type = AssessmentType.PreTest)
    {
        if (!Enum.IsDefined(type)) return BadRequest();
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();
        var nextOrder = (await _context.AssessmentQuestions.Where(q => q.CourseId == courseId && q.Type == type).MaxAsync(q => (int?)q.OrderNumber) ?? 0) + 1;
        ViewBag.Course = course;
        return View("Form", new AssessmentQuestionFormViewModel { CourseId = courseId, Type = type, OrderNumber = nextOrder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentQuestionFormViewModel model)
    {
        if (!Enum.IsDefined(model.Type)) ModelState.AddModelError(nameof(model.Type), "ประเภทข้อสอบไม่ถูกต้อง");
        if (!ModelState.IsValid)
        {
            ViewBag.Course = await _context.Courses.FindAsync(model.CourseId);
            return View("Form", model);
        }

        var question = new AssessmentQuestion
        {
            CourseId = model.CourseId,
            Type = model.Type,
            Text = model.Text.Trim(),
            OrderNumber = model.OrderNumber,
            Options = BuildOptions(model)
        };
        _context.AssessmentQuestions.Add(question);
        await _context.SaveChangesAsync();
        TempData["Success"] = "เพิ่มข้อสอบเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index), new { courseId = model.CourseId, type = model.Type });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var question = await _context.AssessmentQuestions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();
        ViewBag.Course = await _context.Courses.FindAsync(question.CourseId);
        var options = question.Options.OrderBy(o => o.OrderNumber).ToList();
        return View("Form", new AssessmentQuestionFormViewModel
        {
            Id = question.Id,
            CourseId = question.CourseId,
            Type = question.Type,
            Text = question.Text,
            OrderNumber = question.OrderNumber,
            Option1 = options.ElementAtOrDefault(0)?.Text ?? string.Empty,
            Option2 = options.ElementAtOrDefault(1)?.Text ?? string.Empty,
            Option3 = options.ElementAtOrDefault(2)?.Text ?? string.Empty,
            Option4 = options.ElementAtOrDefault(3)?.Text ?? string.Empty,
            CorrectOption = Math.Max(1, options.FindIndex(o => o.IsCorrect) + 1)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssessmentQuestionFormViewModel model)
    {
        var question = await _context.AssessmentQuestions.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == model.Id);
        if (question == null) return NotFound();
        if (question.CourseId != model.CourseId || question.Type != model.Type) return BadRequest();
        if (!ModelState.IsValid)
        {
            ViewBag.Course = await _context.Courses.FindAsync(model.CourseId);
            return View("Form", model);
        }

        question.Text = model.Text.Trim();
        question.OrderNumber = model.OrderNumber;
        _context.AssessmentOptions.RemoveRange(question.Options);
        question.Options = BuildOptions(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "บันทึกการแก้ไขข้อสอบเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index), new { courseId = model.CourseId, type = model.Type });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var question = await _context.AssessmentQuestions.Include(q => q.Course).FirstOrDefaultAsync(q => q.Id == id);
        return question == null ? NotFound() : View(question);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var question = await _context.AssessmentQuestions.FindAsync(id);
        if (question == null) return NotFound();
        var courseId = question.CourseId;
        var type = question.Type;
        _context.AssessmentQuestions.Remove(question);
        await _context.SaveChangesAsync();
        TempData["Success"] = "ลบข้อสอบเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index), new { courseId, type });
    }

    private static List<AssessmentOption> BuildOptions(AssessmentQuestionFormViewModel model) => new()
    {
        new() { Text = model.Option1.Trim(), OrderNumber = 1, IsCorrect = model.CorrectOption == 1 },
        new() { Text = model.Option2.Trim(), OrderNumber = 2, IsCorrect = model.CorrectOption == 2 },
        new() { Text = model.Option3.Trim(), OrderNumber = 3, IsCorrect = model.CorrectOption == 3 },
        new() { Text = model.Option4.Trim(), OrderNumber = 4, IsCorrect = model.CorrectOption == 4 }
    };
}
