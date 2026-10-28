using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class CoursePlayerViewModel
{
    public Course Course { get; set; } = null!;
    public Lesson CurrentLesson { get; set; } = null!;
    public List<Lesson> Lessons { get; set; } = new();
    public HashSet<int> CompletedLessonIds { get; set; } = new();
    public HashSet<int> WatchedLessonIds { get; set; } = new();
    public bool CurrentVideoWatched { get; set; }
    public bool CurrentVideoRequiresWatch { get; set; }
    public bool IsEnrolled { get; set; }
    public int ProgressPercent { get; set; }
    public Lesson? NextLesson { get; set; }
    public Lesson? PreviousLesson { get; set; }
    public bool HasPreTest { get; set; }
    public bool HasPostTest { get; set; }
    public bool CanTakePostTest { get; set; }
    public int? LatestPreTestAttemptId { get; set; }
    public int? LatestPostTestAttemptId { get; set; }
    public int? CertificateId { get; set; }
}
