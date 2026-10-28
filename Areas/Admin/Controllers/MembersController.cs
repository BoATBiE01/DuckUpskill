using DuckUpskill.Data;
using DuckUpskill.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MembersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search)
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.FullName.Contains(search) || (u.Email ?? "").Contains(search));
        }

        ViewBag.Search = search;
        return View(await query.OrderByDescending(u => u.RegisteredAt).ToListAsync());
    }

    public async Task<IActionResult> Details(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        ViewBag.Roles = await _userManager.GetRolesAsync(user);
        ViewBag.Enrollments = await _context.Enrollments.Include(e => e.Course)
            .Where(e => e.UserId == id).OrderByDescending(e => e.EnrolledAt).ToListAsync();
        ViewBag.Orders = await _context.Orders.Include(o => o.Course)
            .Where(o => o.UserId == id).OrderByDescending(o => o.CreatedAt).ToListAsync();
        return View(user);
    }

    public IActionResult Create() => View(new ApplicationUser());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApplicationUser user, string password, bool makeAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(password)) ModelState.AddModelError("password", "กรุณากรอกรหัสผ่าน");
        if (!ModelState.IsValid) return View(user);

        user.UserName = user.Email;
        user.EmailConfirmed = true;
        user.RegisteredAt = DateTime.UtcNow;
        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, makeAdmin ? "Admin" : "Member");
            TempData["Success"] = "เพิ่มสมาชิกสำเร็จแล้ว";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(user);
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        ViewBag.IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ApplicationUser model, bool makeAdmin = false)
    {
        if (id != model.Id) return NotFound();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (!ModelState.IsValid)
        {
            ViewBag.IsAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            return View(model);
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        user.ProfilePictureUrl = model.ProfilePictureUrl;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (makeAdmin && !isAdmin) await _userManager.AddToRoleAsync(user, "Admin");
        if (!makeAdmin && isAdmin) await _userManager.RemoveFromRoleAsync(user, "Admin");
        if (!await _userManager.IsInRoleAsync(user, "Member") && !makeAdmin) await _userManager.AddToRoleAsync(user, "Member");

        TempData["Success"] = "อัปเดตข้อมูลสมาชิกเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == user.Id)
            {
                TempData["Error"] = "ไม่สามารถลบบัญชีผู้ดูแลที่กำลังใช้งานอยู่ได้";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "ลบสมาชิกเรียบร้อยแล้ว";
        }
        return RedirectToAction(nameof(Index));
    }
}
