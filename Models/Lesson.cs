using System.ComponentModel.DataAnnotations;

namespace DuckUpskill.Models;

public class Lesson
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อบทเรียน")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int OrderNumber { get; set; } = 1;

    public int DurationMinutes { get; set; } = 15;

    [StringLength(500)]
    [Display(Name = "ลิงก์วิดีโอ (YouTube URL หรือ Embed)")]
    public string? VideoUrl { get; set; }

    [Display(Name = "เนื้อหา / บันทึกการเรียน (HTML หรือ Markdown)")]
    public string? Content { get; set; }

    [Display(Name = "เปิดให้ทดลองเรียนฟรี")]
    public bool IsPreview { get; set; } = false;

    public ICollection<LessonCompletion> Completions { get; set; } = new List<LessonCompletion>();
}
