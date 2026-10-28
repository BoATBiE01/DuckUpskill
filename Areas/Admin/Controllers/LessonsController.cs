using DuckUpskill.Data;
using DuckUpskill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LessonsController : Controller
{
    private readonly ApplicationDbContext _context;

    public LessonsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.Lessons.OrderBy(l => l.OrderNumber))
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null) return NotFound();

        return View(course);
    }

    public async Task<IActionResult> Create(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        var maxOrder = await _context.Lessons
            .Where(l => l.CourseId == courseId)
            .MaxAsync(l => (int?)l.OrderNumber) ?? 0;

        var lesson = new Lesson
        {
            CourseId = courseId,
            OrderNumber = maxOrder + 1,
            DurationMinutes = 15,
            VideoUrl = "https://www.youtube.com/embed/dQw4w9WgXcQ"
        };

        ViewBag.Course = course;
        return View(lesson);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lesson lesson)
    {
        if (ModelState.IsValid)
        {
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            TempData["Success"] = "เพิ่มบทเรียนสำเร็จแล้ว!";
            return RedirectToAction(nameof(Index), new { courseId = lesson.CourseId });
        }

        ViewBag.Course = await _context.Courses.FindAsync(lesson.CourseId);
        return View(lesson);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();

        ViewBag.Course = lesson.Course;
        return View(lesson);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lesson lesson)
    {
        if (id != lesson.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(lesson);
                await _context.SaveChangesAsync();
                TempData["Success"] = "อัปเดตบทเรียนเรียบร้อยแล้ว!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Lessons.AnyAsync(e => e.Id == lesson.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index), new { courseId = lesson.CourseId });
        }

        ViewBag.Course = await _context.Courses.FindAsync(lesson.CourseId);
        return View(lesson);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var lesson = await _context.Lessons.Include(l => l.Course).FirstOrDefaultAsync(l => l.Id == id);
        if (lesson == null) return NotFound();

        return View(lesson);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson != null)
        {
            var courseId = lesson.CourseId;
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            TempData["Success"] = "ลบบทเรียนเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        return RedirectToAction("Index", "Courses");
    }
}
