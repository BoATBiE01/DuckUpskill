using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuckUpskill.Models;

public enum OrderStatus
{
    [Display(Name = "รอชำระเงิน")]
    Pending,
    [Display(Name = "ชำระเงินเรียบร้อย")]
    Completed,
    [Display(Name = "ยกเลิก")]
    Cancelled
}

public class Order
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(50)]
    public string PaymentMethod { get; set; } = "PromptPay QR";

    public OrderStatus Status { get; set; } = OrderStatus.Completed;

    [StringLength(100)]
    public string? SlipReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}
