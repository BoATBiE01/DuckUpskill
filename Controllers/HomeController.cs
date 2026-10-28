using System.Diagnostics;
using DuckUpskill.Data;
using DuckUpskill.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var courses = await _context.Courses
            .Where(c => c.IsFeatured)
            .OrderByDescending(c => c.CreatedAt)
            .Take(6)
            .ToListAsync();

        ViewBag.TotalCourses = await _context.Courses.CountAsync();
        ViewBag.TotalMembers = await _context.Users.CountAsync();
        ViewBag.TotalEnrollments = await _context.Enrollments.CountAsync();

        return View(courses);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Support()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
