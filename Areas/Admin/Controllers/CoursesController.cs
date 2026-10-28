using DuckUpskill.Data;
using DuckUpskill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CoursesController : Controller
{
    private readonly ApplicationDbContext _context;

    public CoursesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? category, string? filter)
    {
        var query = _context.Courses.Include(c => c.Lessons).Include(c => c.Enrollments).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Title.Contains(search) || c.InstructorName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(c => c.Category == category);
        }

        if (filter == "free")
        {
            query = query.Where(c => c.IsFree);
        }
        else if (filter == "paid")
        {
            query = query.Where(c => !c.IsFree);
        }

        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Filter = filter;
        ViewBag.Categories = await _context.Courses.Select(c => c.Category).Distinct().ToListAsync();

        var courses = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        return View(courses);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var course = await _context.Courses
            .Include(c => c.Lessons.OrderBy(l => l.OrderNumber))
            .Include(c => c.Enrollments).ThenInclude(e => e.User)
            .Include(c => c.Orders).ThenInclude(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (course == null) return NotFound();

        return View(course);
    }

    public IActionResult Create()
    {
        var course = new Course
        {
            Category = "Web Development",
            Level = CourseLevel.Beginner,
            IsFree = true,
            Price = 0,
            Rating = 5.0,
            InstructorName = "Duck Instructor",
            ThumbnailUrl = "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=800&q=80"
        };
        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.Slug))
        {
            course.Slug = GenerateSlug(course.Title);
        }

        // Ensure unique slug
        if (await _context.Courses.AnyAsync(c => c.Slug == course.Slug))
        {
            course.Slug += "-" + Guid.NewGuid().ToString()[..4];
        }

        if (course.IsFree)
        {
            course.Price = 0;
        }

        if (ModelState.IsValid)
        {
            course.CreatedAt = DateTime.UtcNow;
            _context.Add(course);
            await _context.SaveChangesAsync();
            TempData["Success"] = "เพิ่มคอร์สเรียนสำเร็จแล้ว!";
            return RedirectToAction(nameof(Index));
        }

        return View(course);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var course = await _context.Courses.FindAsync(id);
        if (course == null) return NotFound();

        return View(course);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Course course)
    {
        if (id != course.Id) return NotFound();

        if (course.IsFree)
        {
            course.Price = 0;
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(course);
                await _context.SaveChangesAsync();
                TempData["Success"] = "อัปเดตข้อมูลคอร์สเรียนเรียบร้อยแล้ว!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Courses.AnyAsync(e => e.Id == course.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(course);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var course = await _context.Courses
            .Include(c => c.Lessons)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (course == null) return NotFound();

        return View(course);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course != null)
        {
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            TempData["Success"] = "ลบคอร์สเรียนเรียบร้อยแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }

    private static string GenerateSlug(string phrase)
    {
        var str = phrase.ToLowerInvariant().Trim();
        str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", "-");
        str = System.Text.RegularExpressions.Regex.Replace(str, @"[^\w\-]", "");
        return string.IsNullOrWhiteSpace(str) ? "course-" + Guid.NewGuid().ToString()[..6] : str;
    }
}
