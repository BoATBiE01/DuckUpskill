using DuckUpskill.Data;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalCourses = await _context.Courses.CountAsync();
        var totalFree = await _context.Courses.CountAsync(c => c.IsFree);
        var totalPaid = await _context.Courses.CountAsync(c => !c.IsFree);
        var totalMembers = await _context.Users.CountAsync();
        var totalEnrollments = await _context.Enrollments.CountAsync();
        var totalRevenue = await _context.Orders
            .Where(o => o.Status == Models.OrderStatus.Completed)
            .SumAsync(o => (decimal?)o.Amount) ?? 0m;

        var recentOrders = await _context.Orders
            .Include(o => o.Course)
            .Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt)
            .Take(6)
            .ToListAsync();

        var recentEnrollments = await _context.Enrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(6)
            .ToListAsync();

        var popularCourses = await _context.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Lessons)
            .OrderByDescending(c => c.Enrollments.Count)
            .Take(5)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalCourses = totalCourses,
            TotalFreeCourses = totalFree,
            TotalPaidCourses = totalPaid,
            TotalMembers = totalMembers,
            TotalEnrollments = totalEnrollments,
            TotalRevenue = totalRevenue,
            RecentOrders = recentOrders,
            RecentEnrollments = recentEnrollments,
            PopularCourses = popularCourses
        };

        return View(viewModel);
    }
}
