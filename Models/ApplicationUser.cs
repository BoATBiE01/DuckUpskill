using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace DuckUpskill.Models;

public class ApplicationUser : IdentityUser
{
    [PersonalData]
    [StringLength(100)]
    [Display(Name = "ชื่อ-นามสกุล")]
    public string FullName { get; set; } = string.Empty;

    [PersonalData]
    [StringLength(300)]
    public string? ProfilePictureUrl { get; set; }

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = new List<AssessmentAttempt>();
    public ICollection<CourseCertificate> Certificates { get; set; } = new List<CourseCertificate>();
}
