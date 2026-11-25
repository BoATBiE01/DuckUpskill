using System.Diagnostics;
using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

    [HttpGet]
    public async Task<IActionResult> Support()
    {
        var user = await _userManager.GetUserAsync(User);
        var viewModel = new SupportIndexViewModel
        {
            IsAuthenticated = user != null,
            Form = new SupportFormViewModel
            {
                Name = user?.FullName ?? string.Empty,
                Email = user?.Email ?? string.Empty
            }
        };

        if (user != null)
        {
            viewModel.MyTickets = await _context.SupportTickets
                .Include(t => t.Messages)
                .Where(t => t.UserId == user.Id || t.Email == user.Email)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Support(SupportIndexViewModel? viewModel, [Bind(Prefix = "Form")] SupportFormViewModel? form)
    {
        var user = await _userManager.GetUserAsync(User);
        var inputForm = form ?? viewModel?.Form ?? new SupportFormViewModel();

        // Fallback: read directly from Request.Form if model binder didn't catch fields
        if (Request.HasFormContentType)
        {
            if (string.IsNullOrWhiteSpace(inputForm.Name))
                inputForm.Name = Request.Form["Form.Name"].FirstOrDefault() ?? Request.Form["Name"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inputForm.Email))
                inputForm.Email = Request.Form["Form.Email"].FirstOrDefault() ?? Request.Form["Email"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inputForm.Topic))
                inputForm.Topic = Request.Form["Form.Topic"].FirstOrDefault() ?? Request.Form["Topic"].FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(inputForm.Message))
                inputForm.Message = Request.Form["Form.Message"].FirstOrDefault() ?? Request.Form["Message"].FirstOrDefault() ?? string.Empty;
        }

        // Auto-fill logged in user info if fields were left blank
        if (user != null)
        {
            if (string.IsNullOrWhiteSpace(inputForm.Name)) inputForm.Name = user.FullName ?? user.UserName ?? "สมาชิก";
            if (string.IsNullOrWhiteSpace(inputForm.Email)) inputForm.Email = user.Email ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(inputForm.Name) ||
            string.IsNullOrWhiteSpace(inputForm.Email) ||
            string.IsNullOrWhiteSpace(inputForm.Topic) ||
            string.IsNullOrWhiteSpace(inputForm.Message))
        {
            ModelState.AddModelError(string.Empty, "กรุณากรอกข้อมูลให้ครบทุกช่อง");

            var vm = new SupportIndexViewModel
            {
                IsAuthenticated = user != null,
                Form = inputForm,
                MyTickets = user != null
                    ? await _context.SupportTickets
                        .Include(t => t.Messages)
                        .Where(t => t.UserId == user.Id || t.Email == user.Email)
                        .OrderByDescending(t => t.UpdatedAt)
                        .ToListAsync()
                    : new List<SupportTicket>()
            };
            return View(vm);
        }

        var ticket = new SupportTicket
        {
            UserId = user?.Id,
            Name = inputForm.Name.Trim(),
            Email = inputForm.Email.Trim(),
            Topic = inputForm.Topic.Trim(),
            Status = SupportTicketStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<SupportMessage>
            {
                new SupportMessage
                {
                    SenderUserId = user?.Id,
                    SenderName = inputForm.Name.Trim(),
                    IsAdmin = false,
                    Message = inputForm.Message.Trim(),
                    SentAt = DateTime.UtcNow
                }
            }
        };

        _context.SupportTickets.Add(ticket);
        await _context.SaveChangesAsync();

        TempData["Success"] = "ส่งคำถามเรียบร้อยแล้ว! แอดมินได้รับข้อความแล้วและจะตอบกลับโดยเร็วที่สุดครับ 🦆";
        return RedirectToAction(nameof(SupportTicket), new { id = ticket.Id });
    }

    [HttpGet]
    public async Task<IActionResult> SupportTicket(int id)
    {
        var ticket = await _context.SupportTickets
            .Include(t => t.Messages.OrderBy(m => m.SentAt))
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        // Allow access if admin, or owner of ticket, or guest ticket
        if (!isAdmin && user != null && ticket.UserId != null && ticket.UserId != user.Id && ticket.Email != user.Email)
        {
            return Forbid();
        }

        var viewModel = new SupportTicketDetailViewModel
        {
            Ticket = ticket,
            IsAdmin = isAdmin
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReplySupportTicket(int ticketId, string replyMessage)
    {
        if (string.IsNullOrWhiteSpace(replyMessage))
        {
            TempData["Error"] = "กรุณากรอกข้อความตอบกลับ";
            return RedirectToAction(nameof(SupportTicket), new { id = ticketId });
        }

        var ticket = await _context.SupportTickets
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == ticketId);

        if (ticket == null)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        var senderName = isAdmin
            ? "Duck Support (Admin)"
            : (user?.FullName ?? ticket.Name);

        var message = new SupportMessage
        {
            TicketId = ticket.Id,
            SenderUserId = user?.Id,
            SenderName = senderName,
            IsAdmin = isAdmin,
            Message = replyMessage.Trim(),
            SentAt = DateTime.UtcNow
        };

        _context.SupportMessages.Add(message);

        // Update ticket status
        ticket.Status = isAdmin ? SupportTicketStatus.Replied : SupportTicketStatus.Pending;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["Success"] = isAdmin
            ? "ส่งข้อความตอบกลับผู้เรียนเรียบร้อยแล้ว"
            : "ส่งข้อความตอบกลับแอดมินเรียบร้อยแล้ว";

        if (isAdmin)
        {
            return RedirectToAction("Details", "Support", new { area = "Admin", id = ticketId });
        }

        return RedirectToAction(nameof(SupportTicket), new { id = ticketId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSupportTicket(int ticketId)
    {
        var ticket = await _context.SupportTickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();

        var user = await _userManager.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && user != null && ticket.UserId != null && ticket.UserId != user.Id)
        {
            return Forbid();
        }

        ticket.Status = SupportTicketStatus.Closed;
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["Success"] = "ปิดการสนทนาเรียบร้อยแล้ว หากมีข้อสงสัยใหม่สามารถเปิดคำถามใหม่ได้ตลอดเวลาครับ";
        return RedirectToAction(nameof(SupportTicket), new { id = ticketId });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
