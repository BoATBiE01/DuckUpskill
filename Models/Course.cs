using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuckUpskill.Models;

public enum CourseLevel
{
    [Display(Name = "ผู้เริ่มต้น (Beginner)")]
    Beginner,
    [Display(Name = "ระดับกลาง (Intermediate)")]
    Intermediate,
    [Display(Name = "ระดับสูง (Advanced)")]
    Advanced
}

public class Course
{
    public int Id { get; set; }

    [Required(ErrorMessage = "กรุณากรอกชื่อคอร์ส")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(220)]
    public string Slug { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกคำอธิบายย่อ")]
    [StringLength(400)]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรายละเอียดเนื้อหา")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500)]
    public string ThumbnailUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Category { get; set; } = "Web Development";

    public CourseLevel Level { get; set; } = CourseLevel.Beginner;

    public bool IsFree { get; set; } = true;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 99999)]
    public decimal Price { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 99999)]
    public decimal? OriginalPrice { get; set; }

    [Required]
    [StringLength(120)]
    public string InstructorName { get; set; } = "Duck Coach";

    [StringLength(200)]
    public string InstructorTitle { get; set; } = "Senior Instructor";

    [StringLength(500)]
    public string InstructorAvatarUrl { get; set; } = "";

    public int DurationHours { get; set; } = 5;

    public double Rating { get; set; } = 4.9;

    public int ReviewCount { get; set; } = 0;

    public bool IsFeatured { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = new List<AssessmentQuestion>();
    public ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = new List<AssessmentAttempt>();
    public ICollection<CourseCertificate> Certificates { get; set; } = new List<CourseCertificate>();

    [Range(1, 100)]
    public int CertificatePassingScore { get; set; } = 80;

    [NotMapped]
    public int TotalLessons => Lessons?.Count ?? 0;
}
