using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class CourseDetailsViewModel
{
    public Course Course { get; set; } = null!;
    public bool IsEnrolled { get; set; }
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
    public int EnrolledCount { get; set; }
    public int CompletedCount { get; set; }
    public int ProgressPercent { get; set; }
}
