using System.ComponentModel.DataAnnotations;
using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class SupportFormViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อของคุณ")]
    [StringLength(100, ErrorMessage = "ชื่อต้องไม่เกิน 100 ตัวอักษร")]
    [Display(Name = "ชื่อของคุณ")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(256)]
    [Display(Name = "อีเมลติดต่อกลับ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกหรือระบุหัวข้อ")]
    [StringLength(150, ErrorMessage = "หัวข้อต้องไม่เกิน 150 ตัวอักษร")]
    [Display(Name = "หัวข้อที่ต้องการติดต่อ")]
    public string Topic { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรายละเอียดปัญหาหรือคำถาม")]
    [StringLength(4000, ErrorMessage = "ข้อความยาวเกินกำหนด")]
    [Display(Name = "รายละเอียด")]
    public string Message { get; set; } = string.Empty;
}

public class SupportIndexViewModel
{
    public SupportFormViewModel Form { get; set; } = new();
    public List<SupportTicket> MyTickets { get; set; } = new();
    public bool IsAuthenticated { get; set; }
}

public class SupportTicketDetailViewModel
{
    public SupportTicket Ticket { get; set; } = null!;
    public bool IsAdmin { get; set; }

    [Required(ErrorMessage = "กรุณากรอกข้อความตอบกลับ")]
    [StringLength(4000, ErrorMessage = "ข้อความยาวเกินกำหนด")]
    [Display(Name = "ข้อความตอบกลับ")]
    public string ReplyMessage { get; set; } = string.Empty;
}
