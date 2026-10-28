using DuckUpskill.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<LessonCompletion> LessonCompletions { get; set; } = null!;
    public DbSet<LessonVideoWatch> LessonVideoWatches { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<AssessmentQuestion> AssessmentQuestions { get; set; } = null!;
    public DbSet<AssessmentOption> AssessmentOptions { get; set; } = null!;
    public DbSet<AssessmentAttempt> AssessmentAttempts { get; set; } = null!;
    public DbSet<AssessmentAnswer> AssessmentAnswers { get; set; } = null!;
    public DbSet<CourseCertificate> CourseCertificates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Lesson>()
            .HasOne(l => l.Course)
            .WithMany(c => c.Lessons)
            .HasForeignKey(l => l.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Enrollment>()
            .HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Enrollment>()
            .HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Enrollment>()
            .HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique();

        builder.Entity<LessonCompletion>()
            .HasOne(lc => lc.Lesson)
            .WithMany(l => l.Completions)
            .HasForeignKey(lc => lc.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LessonCompletion>()
            .HasOne(lc => lc.User)
            .WithMany()
            .HasForeignKey(lc => lc.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<LessonCompletion>()
            .HasIndex(lc => new { lc.UserId, lc.LessonId })
            .IsUnique();

        builder.Entity<LessonVideoWatch>()
            .HasOne(w => w.Lesson)
            .WithMany()
            .HasForeignKey(w => w.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LessonVideoWatch>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<LessonVideoWatch>()
            .HasIndex(w => new { w.UserId, w.LessonId })
            .IsUnique();

        builder.Entity<AssessmentQuestion>()
            .HasOne(q => q.Course)
            .WithMany(c => c.AssessmentQuestions)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssessmentOption>()
            .HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.AssessmentQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssessmentAttempt>()
            .HasOne(a => a.Course)
            .WithMany(c => c.AssessmentAttempts)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssessmentAttempt>()
            .HasOne(a => a.User)
            .WithMany(u => u.AssessmentAttempts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<AssessmentAnswer>()
            .HasOne(a => a.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(a => a.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssessmentAnswer>()
            .HasOne(a => a.Question)
            .WithMany()
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<AssessmentAnswer>()
            .HasOne(a => a.SelectedOption)
            .WithMany()
            .HasForeignKey(a => a.SelectedOptionId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<CourseCertificate>()
            .HasOne(c => c.Course)
            .WithMany(c => c.Certificates)
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<CourseCertificate>()
            .HasOne(c => c.User)
            .WithMany(u => u.Certificates)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.Entity<CourseCertificate>()
            .HasIndex(c => new { c.UserId, c.CourseId })
            .IsUnique();
        builder.Entity<CourseCertificate>()
            .HasIndex(c => c.CertificateNumber)
            .IsUnique();

        builder.Entity<Order>()
            .HasOne(o => o.Course)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
