using DuckUpskill.Data;
using DuckUpskill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(OrderStatus? status, string? search)
    {
        var query = _context.Orders
            .Include(o => o.Course)
            .Include(o => o.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.OrderNumber.Contains(search) || (o.User != null && (o.User.FullName.Contains(search) || o.User.Email!.Contains(search))));
        }

        ViewBag.SelectedStatus = status;
        ViewBag.Search = search;

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(orders);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Course)
            .Include(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Course)
            .Include(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderStatus status, string? slipReference)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        order.SlipReference = slipReference;
        if (status == OrderStatus.Completed && order.PaidAt == null)
        {
            order.PaidAt = DateTime.UtcNow;

            // Ensure enrollment exists
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.UserId == order.UserId && e.CourseId == order.CourseId);
            if (!isEnrolled)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    UserId = order.UserId,
                    CourseId = order.CourseId,
                    EnrolledAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "อัปเดตสถานะคำสั่งซื้อเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var order = await _context.Orders
            .Include(o => o.Course)
            .Include(o => o.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order != null)
        {
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            TempData["Success"] = "ลบรายการคำสั่งซื้อเรียบร้อยแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }
}
