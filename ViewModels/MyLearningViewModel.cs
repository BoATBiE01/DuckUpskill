using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class MyLearningViewModel
{
    public ApplicationUser User { get; set; } = null!;
    public List<EnrolledCourseItem> EnrolledCourses { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
}

public class EnrolledCourseItem
{
    public Enrollment Enrollment { get; set; } = null!;
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public int ProgressPercent { get; set; }
    public Lesson? NextLesson { get; set; }
    public bool HasPreTest { get; set; }
    public bool IsCourseCompleted { get; set; }
    public CourseCertificate? Certificate { get; set; }
}
