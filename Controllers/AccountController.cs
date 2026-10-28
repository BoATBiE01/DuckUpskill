using DuckUpskill.Data;
using DuckUpskill.Models;
using DuckUpskill.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace DuckUpskill.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("MyCourses", "Learning");
            }
        }

        ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านสมาชิกไม่ถูกต้อง");
        return View(model);
    }

    [HttpGet]
    public IActionResult AdminLogin(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
        {
            ModelState.AddModelError(string.Empty, "บัญชีนี้ไม่มีสิทธิ์เข้าถึงส่วนงานผู้ดูแลระบบ");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        ModelState.AddModelError(string.Empty, "อีเมลหรือรหัสผ่านผู้ดูแลระบบไม่ถูกต้อง");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            RegisteredAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Member");
            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["Success"] = "ยินดีต้อนรับสู่ DuckUpskill! สมัครสมาชิกสำเร็จแล้ว 🦆";
            return RedirectToAction("MyCourses", "Learning");
        }

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "ออกจากระบบเรียบร้อยแล้ว";
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Member")]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var enrollments = await _context.Enrollments
            .Where(e => e.UserId == user.Id)
            .Select(e => new { e.CourseId, Total = e.Course!.Lessons.Count, Completed = e.Course.Lessons.Count(l => l.Completions.Any(c => c.UserId == user.Id)) })
            .ToListAsync();

        var model = new MemberProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            ProfilePictureUrl = user.ProfilePictureUrl,
            RegisteredAt = user.RegisteredAt,
            EnrolledCourseCount = enrollments.Count,
            CompletedCourseCount = enrollments.Count(e => e.Total > 0 && e.Completed >= e.Total)
        };

        return View(model);
    }

    [Authorize(Roles = "Member")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(MemberProfileViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Profile image URLs are no longer accepted from form fields; uploads use validated files only.
        ModelState.Remove(nameof(model.ProfilePictureUrl));

        if (model.ProfilePicture is { Length: > 0 })
        {
            var extension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            var allowedTypes = new Dictionary<string, string>
            {
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".png"] = "image/png",
                [".webp"] = "image/webp"
            };

            if (!allowedTypes.TryGetValue(extension, out var expectedContentType) || model.ProfilePicture.ContentType != expectedContentType)
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "รองรับเฉพาะไฟล์รูป JPG, PNG หรือ WebP เท่านั้น");
            }
            else if (model.ProfilePicture.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.ProfilePicture), "รูปโปรไฟล์ต้องมีขนาดไม่เกิน 5 MB");
            }
            else
            {
                await using var imageStream = model.ProfilePicture.OpenReadStream();
                var signature = new byte[12];
                var bytesRead = await imageStream.ReadAsync(signature);
                if (!HasValidImageSignature(signature, bytesRead, extension))
                {
                    ModelState.AddModelError(nameof(model.ProfilePicture), "ไฟล์ที่เลือกไม่ใช่รูปภาพที่รองรับ หรือไฟล์เสียหาย");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateProfileStatsAsync(model, user.Id);
            model.ProfilePictureUrl = user.ProfilePictureUrl;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        var oldPicturePath = user.ProfilePictureUrl;
        if (model.ProfilePicture is { Length: > 0 })
        {
            var uploadDirectory = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadDirectory);
            var extension = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDirectory, fileName);
            await using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await model.ProfilePicture.CopyToAsync(output);
            }
            user.ProfilePictureUrl = $"/uploads/profiles/{fileName}";
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (user.ProfilePictureUrl != oldPicturePath)
            {
                DeleteProfilePicture(user.ProfilePictureUrl);
                user.ProfilePictureUrl = oldPicturePath;
            }
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            await PopulateProfileStatsAsync(model, user.Id);
            model.ProfilePictureUrl = oldPicturePath;
            return View(model);
        }

        if (oldPicturePath != user.ProfilePictureUrl)
        {
            DeleteProfilePicture(oldPicturePath);
        }

        TempData["Success"] = "บันทึกข้อมูลโปรไฟล์เรียบร้อยแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    private static bool HasValidImageSignature(byte[] bytes, int length, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            ".png" => length >= 8 && bytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50,
            _ => false
        };
    }

    private void DeleteProfilePicture(string? pictureUrl)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl) || !pictureUrl.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase)) return;
        var fileName = Path.GetFileName(pictureUrl);
        var fullPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles", fileName);
        if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
    }

    [Authorize(Roles = "Member")]
    [HttpGet]
    public IActionResult DeleteAccount() => View(new DeleteAccountViewModel());

    [Authorize(Roles = "Member")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!string.Equals(model.Confirmation, "DELETE", StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Confirmation), "กรุณาพิมพ์ DELETE เพื่อยืนยันการลบบัญชี");
            return View(model);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!passwordValid)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "รหัสผ่านปัจจุบันไม่ถูกต้อง");
            return View(model);
        }

        var pictureUrl = user.ProfilePictureUrl;
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Remove application data explicitly before deleting the Identity user.
            // This also works with SQL Server databases created before the FK rules were updated.
            var attempts = await _context.AssessmentAttempts
                .Where(a => a.UserId == user.Id)
                .ToListAsync();
            var attemptIds = attempts.Select(a => a.Id).ToList();
            if (attemptIds.Count > 0)
            {
                var answers = await _context.AssessmentAnswers
                    .Where(a => attemptIds.Contains(a.AttemptId))
                    .ToListAsync();
                _context.AssessmentAnswers.RemoveRange(answers);
                _context.AssessmentAttempts.RemoveRange(attempts);
            }

            _context.CourseCertificates.RemoveRange(await _context.CourseCertificates.Where(c => c.UserId == user.Id).ToListAsync());
            _context.LessonVideoWatches.RemoveRange(await _context.LessonVideoWatches.Where(w => w.UserId == user.Id).ToListAsync());
            _context.LessonCompletions.RemoveRange(await _context.LessonCompletions.Where(c => c.UserId == user.Id).ToListAsync());
            _context.Enrollments.RemoveRange(await _context.Enrollments.Where(e => e.UserId == user.Id).ToListAsync());
            _context.Orders.RemoveRange(await _context.Orders.Where(o => o.UserId == user.Id).ToListAsync());
            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
                await transaction.RollbackAsync();
                return View(model);
            }

            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            ModelState.AddModelError(string.Empty, "ไม่สามารถลบบัญชีได้ เนื่องจากยังมีข้อมูลที่เชื่อมโยงอยู่ กรุณาติดต่อผู้ดูแลระบบ");
            return View(model);
        }

        await _signInManager.SignOutAsync();
        DeleteProfilePicture(pictureUrl);
        TempData["Success"] = "ลบบัญชีสมาชิกเรียบร้อยแล้ว";
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Member")]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());



    [Authorize(Roles = "Member")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "เปลี่ยนรหัสผ่านเรียบร้อยแล้ว";
        return RedirectToAction(nameof(Profile));
    }

    private async Task PopulateProfileStatsAsync(MemberProfileViewModel model, string userId)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.UserId == userId)
            .Select(e => new { Total = e.Course!.Lessons.Count, Completed = e.Course.Lessons.Count(l => l.Completions.Any(c => c.UserId == userId)) })
            .ToListAsync();
        model.EnrolledCourseCount = enrollments.Count;
        model.CompletedCourseCount = enrollments.Count(e => e.Total > 0 && e.Completed >= e.Total);
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
