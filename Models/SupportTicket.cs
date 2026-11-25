using System.ComponentModel.DataAnnotations;

namespace DuckUpskill.Models;

public enum SupportTicketStatus
{
    [Display(Name = "รอการตอบกลับ")]
    Pending = 0,

    [Display(Name = "ตอบกลับแล้ว")]
    Replied = 1,

    [Display(Name = "ปิดการสนทนา")]
    Closed = 2
}

public class SupportTicket
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อของคุณ")]
    [StringLength(100, ErrorMessage = "ชื่อต้องไม่เกิน 100 ตัวอักษร")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณาเลือกหรือระบุหัวข้อ")]
    [StringLength(150, ErrorMessage = "หัวข้อต้องไม่เกิน 150 ตัวอักษร")]
    public string Topic { get; set; } = string.Empty;

    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
}

public class SupportMessage
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    public string? SenderUserId { get; set; }
    public ApplicationUser? SenderUser { get; set; }

    [Required]
    [StringLength(100)]
    public string SenderName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    [Required(ErrorMessage = "กรุณากรอกข้อความ")]
    [StringLength(4000, ErrorMessage = "ข้อความยาวเกินกำหนด")]
    public string Message { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
