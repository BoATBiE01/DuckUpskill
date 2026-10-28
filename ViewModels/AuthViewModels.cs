using System.ComponentModel.DataAnnotations;

namespace DuckUpskill.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "จดจำการเข้าสู่ระบบ")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ-นามสกุล")]
    [StringLength(100, ErrorMessage = "ชื่อต้องมีความยาวไม่เกิน 100 ตัวอักษร")]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [StringLength(100, ErrorMessage = "รหัสผ่านต้องมีความยาวอย่างน้อย {2} ตัวอักษร", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "ยืนยันรหัสผ่าน")]
    [Compare("Password", ErrorMessage = "รหัสผ่านและการยืนยันรหัสผ่านไม่ตรงกัน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
