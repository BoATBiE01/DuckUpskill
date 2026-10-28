using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Checkout(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // If already enrolled, redirect to course
        var isEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == courseId);
        if (isEnrolled)
        {
            TempData["Info"] = "คุณได้ลงทะเบียนคอร์สนี้ไว้แล้ว สามารถเริ่มเรียนได้ทันที!";
            return RedirectToAction("Learn", "Courses", new { id = course.Slug });
        }

        var model = new CheckoutViewModel
        {
            Course = course,
            PaymentMethod = "PromptPay QR"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckout(int courseId, string paymentMethod, string? accountName, string? slipNotes)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // Generate Order
        var orderNumber = $"DUCK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            UserId = user.Id,
            CourseId = courseId,
            Amount = course.Price,
            PaymentMethod = paymentMethod ?? "PromptPay QR",
            Status = OrderStatus.Completed,
            SlipReference = string.IsNullOrWhiteSpace(slipNotes) ? "MOCK-TXN-" + Guid.NewGuid().ToString()[..8].ToUpper() : slipNotes,
            CreatedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // Auto enroll
        var isEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == user.Id && e.CourseId == courseId);
        if (!isEnrolled)
        {
            _context.Enrollments.Add(new Enrollment
            {
                UserId = user.Id,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"ชำระเงินจำลองสำเร็จเรียบร้อย! ขอบคุณที่ร่วมอัปสกิลกับ DuckUpskill (เลขที่คำสั่งซื้อ: {orderNumber})";
        return RedirectToAction("Success", new { id = order.Id });
    }

    public async Task<IActionResult> Success(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var order = await _context.Orders
            .Include(o => o.Course)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var orders = await _context.Orders
            .Include(o => o.Course)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }
}
