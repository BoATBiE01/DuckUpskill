using System.ComponentModel.DataAnnotations;

namespace DuckUpskill.Models;

public class LessonVideoWatch
{
    public int Id { get; set; }

    [Required]
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
}
