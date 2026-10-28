using System.ComponentModel.DataAnnotations;

namespace DuckUpskill.Models;

public class LessonCompletion
{
    public int Id { get; set; }

    [Required]
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
