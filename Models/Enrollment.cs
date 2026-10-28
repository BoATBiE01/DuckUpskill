using System.ComponentModel.DataAnnotations;

namespace DuckUpskill.Models;

public class Enrollment
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    public ICollection<LessonCompletion> LessonCompletions { get; set; } = new List<LessonCompletion>();
}
