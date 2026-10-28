using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DuckUpskill.Models;

public enum AssessmentType
{
    PreTest,
    PostTest
}

public class AssessmentQuestion
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    public AssessmentType Type { get; set; }

    [Required]
    [StringLength(1000)]
    public string Text { get; set; } = string.Empty;

    public int OrderNumber { get; set; }

    public ICollection<AssessmentOption> Options { get; set; } = new List<AssessmentOption>();
}

public class AssessmentOption
{
    public int Id { get; set; }

    [Required]
    public int AssessmentQuestionId { get; set; }
    public AssessmentQuestion? Question { get; set; }

    [Required]
    [StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }
    public int OrderNumber { get; set; }
}

public class AssessmentAttempt
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public AssessmentType Type { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public bool Passed { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AssessmentAnswer> Answers { get; set; } = new List<AssessmentAnswer>();
}

public class AssessmentAnswer
{
    public int Id { get; set; }

    [Required]
    public int AttemptId { get; set; }
    public AssessmentAttempt? Attempt { get; set; }

    [Required]
    public int QuestionId { get; set; }
    public AssessmentQuestion? Question { get; set; }

    public int? SelectedOptionId { get; set; }
    public AssessmentOption? SelectedOption { get; set; }
    public bool IsCorrect { get; set; }
}

public class CourseCertificate
{
    public int Id { get; set; }

    [Required]
    public int CourseId { get; set; }
    public Course? Course { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(40)]
    public string CertificateNumber { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}
