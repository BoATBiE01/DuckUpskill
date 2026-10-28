using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DuckUpskill.ViewModels;

public class MemberProfileViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [StringLength(100, ErrorMessage = "ชื่อต้องไม่เกิน 100 ตัวอักษร")]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง")]
    [StringLength(32)]
    [Display(Name = "เบอร์โทรศัพท์")]
    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }

    [Display(Name = "อัปโหลดรูปโปรไฟล์")]
    public IFormFile? ProfilePicture { get; set; }

    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public int EnrolledCourseCount { get; set; }
    public int CompletedCourseCount { get; set; }
}

public class DeleteAccountViewModel
{
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านปัจจุบัน")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาพิมพ์ DELETE เพื่อยืนยัน")]
    [Display(Name = "พิมพ์ DELETE เพื่อยืนยัน")]
    [RegularExpression("DELETE", ErrorMessage = "กรุณาพิมพ์ DELETE ให้ตรงตามที่กำหนด")]
    public string Confirmation { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านปัจจุบัน")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านปัจจุบัน")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่านใหม่")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "รหัสผ่านใหม่ต้องมีอย่างน้อย 8 ตัวอักษร")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่านใหม่")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่านใหม่")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "รหัสผ่านใหม่ไม่ตรงกัน")]
    [Display(Name = "ยืนยันรหัสผ่านใหม่")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
