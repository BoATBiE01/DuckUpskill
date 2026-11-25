using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SupportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(SupportTicketStatus? status, string? search)
    {
        var query = _context.SupportTickets
            .Include(t => t.Messages)
            .Include(t => t.User)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(t => t.Name.Contains(s) || t.Email.Contains(s) || t.Topic.Contains(s));
        }

        ViewBag.SelectedStatus = status;
        ViewBag.Search = search;
        ViewBag.PendingCount = await _context.SupportTickets.CountAsync(t => t.Status == SupportTicketStatus.Pending);
        ViewBag.RepliedCount = await _context.SupportTickets.CountAsync(t => t.Status == SupportTicketStatus.Replied);
        ViewBag.ClosedCount = await _context.SupportTickets.CountAsync(t => t.Status == SupportTicketStatus.Closed);

        // Sort: Pending first, then by most recently updated
        var tickets = await query
            .OrderBy(t => t.Status == SupportTicketStatus.Pending ? 0 : 1)
            .ThenByDescending(t => t.UpdatedAt)
            .ToListAsync();

        return View(tickets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ticket = await _context.SupportTickets
            .Include(t => t.Messages.OrderBy(m => m.SentAt))
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        var viewModel = new SupportTicketDetailViewModel
        {
            Ticket = ticket,
            IsAdmin = true
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int ticketId, string replyMessage)
    {
        if (string.IsNullOrWhiteSpace(replyMessage))
        {
            TempData["Error"] = "กรุณากรอกข้อความตอบกลับ";
            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();

        var adminUser = await _userManager.GetUserAsync(User);

        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderUserId = adminUser?.Id,
            SenderName = "Duck Support (Admin)",
            IsAdmin = true,
            Message = replyMessage.Trim(),
            SentAt = DateTime.UtcNow
        };

        _context.SupportMessages.Add(message);

        // Mark status as Replied
        ticket.Status = SupportTicketStatus.Replied;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = "ส่งข้อความตอบกลับผู้เรียนเรียบร้อยแล้ว 🦆";
        return RedirectToAction(nameof(Details), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, SupportTicketStatus status)
    {
        var ticket = await _context.SupportTickets.FindAsync(id);
        if (ticket == null) return NotFound();

        ticket.Status = status;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "อัปเดตสถานะการสนทนาเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ticket = await _context.SupportTickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket != null)
        {
            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();
            TempData["Success"] = "ลบรายการคำถามเรียบร้อยแล้ว";
        }

        return RedirectToAction(nameof(Index));
    }
}
