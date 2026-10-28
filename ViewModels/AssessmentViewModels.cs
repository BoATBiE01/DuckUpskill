using System.ComponentModel.DataAnnotations;
using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class AssessmentViewModel
{
    public Course Course { get; set; } = null!;
    public AssessmentType Type { get; set; }
    public List<AssessmentQuestion> Questions { get; set; } = new();
}

public class SubmitAssessmentViewModel
{
    public int CourseId { get; set; }
    public AssessmentType Type { get; set; }
    public Dictionary<int, int> SelectedOptions { get; set; } = new();
}

public class AssessmentResultViewModel
{
    public Course Course { get; set; } = null!;
    public AssessmentAttempt Attempt { get; set; } = null!;
    public CourseCertificate? Certificate { get; set; }
}

public class AssessmentQuestionFormViewModel
{
    public int Id { get; set; }
    [Required]
    public int CourseId { get; set; }
    public AssessmentType Type { get; set; }
    [Required(ErrorMessage = "กรุณากรอกโจทย์")]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;
    [Range(1, 999)]
    public int OrderNumber { get; set; } = 1;
    [Required] public string Option1 { get; set; } = string.Empty;
    [Required] public string Option2 { get; set; } = string.Empty;
    [Required] public string Option3 { get; set; } = string.Empty;
    [Required] public string Option4 { get; set; } = string.Empty;
    [Range(1, 4, ErrorMessage = "เลือกคำตอบที่ถูกต้อง 1–4")]
    public int CorrectOption { get; set; } = 1;
}
