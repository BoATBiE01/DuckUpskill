using DuckUpskill.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.EnsureCreatedAsync();
        await EnsureExtraSchemaAsync(context);

        foreach (var roleName in new[] { "Admin", "Member" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            await SeedUserAsync(userManager, "admin@duckupskill.com", "Admin@Duck123!", "Duck Master (Administrator)", "Admin");
            await SeedUserAsync(userManager, "student@duckupskill.com", "Student@Duck123!", "น้องเป็ด ขยันเรียน", "Member");
            await SeedAssessmentQuestionsAsync(context);
            await NormalizeGeneratedAssessmentChoicesAsync(context);
            await RandomizeAssessmentOptionOrderAsync(context);
        }

        Console.WriteLine("[DuckUpskill] ฐานข้อมูลและข้อมูลพื้นฐานพร้อมใช้งาน");
    }

    private static async Task SeedUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                RegisteredAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"ไม่สามารถสร้างผู้ใช้เริ่มต้น {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }
    }

    private static async Task SeedAssessmentQuestionsAsync(ApplicationDbContext context)
    {
        var courseIds = await context.Courses.Select(c => c.Id).ToListAsync();
        foreach (var courseId in courseIds)
        {
            var lessons = await context.Lessons.Where(l => l.CourseId == courseId).OrderBy(l => l.OrderNumber).ToListAsync();
            if (lessons.Count == 0) continue;

            foreach (var type in new[] { AssessmentType.PreTest, AssessmentType.PostTest })
            {
                if (await context.AssessmentQuestions.AnyAsync(q => q.CourseId == courseId && q.Type == type)) continue;

                var questions = lessons.Take(5).Select((lesson, index) => new AssessmentQuestion
                {
                    CourseId = courseId,
                    Type = type,
                    OrderNumber = index + 1,
                    Text = type == AssessmentType.PreTest
                        ? $"[ก่อนเรียน] หัวข้อใดตรงกับบทเรียน “{CleanLessonTitle(lesson.Title)}”?"
                        : $"[หลังเรียน] หัวข้อใดตรงกับบทเรียน “{CleanLessonTitle(lesson.Title)}”?",
                    Options = new List<AssessmentOption>
                    {
                        new() { Text = CleanLessonTitle(lesson.Title), OrderNumber = 1, IsCorrect = true },
                        new() { Text = "หัวข้อจากบทเรียนอื่น", OrderNumber = 2, IsCorrect = false },
                        new() { Text = "เนื้อหาที่ไม่ได้กล่าวถึง", OrderNumber = 3, IsCorrect = false },
                        new() { Text = "แนวคิดคนละหัวข้อ", OrderNumber = 4, IsCorrect = false }
                    }
                }).ToList();

                await context.AssessmentQuestions.AddRangeAsync(questions);
            }
        }
        await context.SaveChangesAsync();
    }

    private static async Task NormalizeGeneratedAssessmentChoicesAsync(ApplicationDbContext context)
    {
        // Normalize old generated questions too (older versions used different text prefixes).
        // Leave administrator-authored questions untouched.
        var questions = await context.AssessmentQuestions
            .Where(q => q.Type == AssessmentType.PreTest || q.Type == AssessmentType.PostTest)
            .Include(q => q.Options)
            .OrderBy(q => q.CourseId)
            .ThenBy(q => q.Type)
            .ThenBy(q => q.OrderNumber)
            .ToListAsync();

        foreach (var group in questions
            .Where(q => IsGeneratedAssessmentQuestion(q.Text))
            .GroupBy(q => new { q.CourseId, q.Type }))
        {
            foreach (var question in group)
            {
                var options = question.Options.OrderBy(o => o.Id).ToList();
                if (options.Count < 2 || options.Count(o => o.IsCorrect) != 1) continue;

                // Randomize answer order on startup so generated questions do not always place the answer first.
                var shuffledOptions = options.OrderBy(_ => Random.Shared.Next()).ToList();
                for (var index = 0; index < shuffledOptions.Count; index++)
                {
                    shuffledOptions[index].OrderNumber = index + 1;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task RandomizeAssessmentOptionOrderAsync(ApplicationDbContext context)
    {
        var attempts = await context.AssessmentAttempts
            .Select(a => new { a.UserId, a.CourseId, a.Type })
            .Distinct()
            .ToListAsync();
        var taken = attempts.ToHashSet();

        var groups = await context.AssessmentQuestions
            .Include(q => q.Options)
            .Where(q => q.Type == AssessmentType.PreTest || q.Type == AssessmentType.PostTest)
            .ToListAsync();

        foreach (var group in groups.GroupBy(q => new { q.CourseId, q.Type }))
        {
            var questions = group.OrderBy(q => q.OrderNumber).ToList();
            var rng = Random.Shared;
            var correctOrder = Enumerable.Range(0, questions.Count).ToList();
            Shuffle(correctOrder, rng);

            // Keep deterministic saved order, but randomize the right answer position per question once.
            for (var index = 0; index < questions.Count; index++)
            {
                var question = questions[index];
                if (!IsGeneratedAssessmentQuestion(question.Text)) continue;
                var options = question.Options.OrderBy(o => o.OrderNumber).ToList();
                if (options.Count < 2 || options.Count(o => o.IsCorrect) != 1) continue;

                var correctOption = options.Single(o => o.IsCorrect);
                var wrongOptions = options.Where(o => !o.IsCorrect).ToList();
                var position = correctOrder[index] + 1;
                correctOption.OrderNumber = position;
                var next = 1;
                foreach (var option in wrongOptions)
                {
                    while (next == position) next++;
                    option.OrderNumber = next++;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static void Shuffle<T>(IList<T> items, Random random)
    {
        for (var index = items.Count - 1; index > 0; index--)
        {
            var swapIndex = random.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }

    private static bool IsGeneratedAssessmentQuestion(string text) =>
        text.StartsWith("[ก่อนเรียน]", StringComparison.Ordinal) ||
        text.StartsWith("[หลังเรียน]", StringComparison.Ordinal) ||
        text.StartsWith("[ประเมินก่อนเรียน:", StringComparison.Ordinal) ||
        text.StartsWith("[ทบทวนหลังเรียน:", StringComparison.Ordinal);

    private static string CleanLessonTitle(string title)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(title.Trim(), @"^\d+\.\s*", "");
        var colonIndex = cleaned.IndexOf(':');
        return colonIndex >= 0 ? cleaned[(colonIndex + 1)..].Trim() : cleaned;
    }

    private static List<string> BuildDistractors(string topic, List<Lesson> lessons, int lessonIndex)
    {
        var choices = lessons
            .Where((_, index) => index != lessonIndex)
            .Select(lesson => CleanLessonTitle(lesson.Title))
            .Where(title => !string.Equals(title, topic, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var fallback = new[] { "หัวข้อที่ไม่ได้กล่าวถึงในบทเรียนนี้", "การประยุกต์ใช้เนื้อหาจากบทเรียนอื่น", "การทบทวนแนวคิดคนละหัวข้อ" };
        foreach (var item in fallback)
        {
            if (choices.Count >= 3) break;
            if (!choices.Contains(item, StringComparer.OrdinalIgnoreCase)) choices.Add(item);
        }
        while (choices.Count < 3) choices.Add($"ตัวเลือกที่ไม่เกี่ยวข้อง {choices.Count + 1}");
        return choices.Take(3).ToList();
    }

    private static async Task EnsureExtraSchemaAsync(ApplicationDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH('dbo.Courses', 'CertificatePassingScore') IS NULL
    ALTER TABLE dbo.Courses ADD CertificatePassingScore int NOT NULL CONSTRAINT DF_Courses_CertificatePassingScore DEFAULT 80;

IF OBJECT_ID('dbo.LessonVideoWatches', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LessonVideoWatches (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LessonId int NOT NULL,
        UserId nvarchar(450) NOT NULL,
        WatchedAt datetime2 NOT NULL,
        CONSTRAINT FK_LessonVideoWatches_Lessons FOREIGN KEY (LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE,
        CONSTRAINT FK_LessonVideoWatches_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_LessonVideoWatches_UserId_LessonId ON dbo.LessonVideoWatches(UserId, LessonId);
END;

IF OBJECT_ID('dbo.AssessmentQuestions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssessmentQuestions (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CourseId int NOT NULL,
        Type int NOT NULL,
        Text nvarchar(1000) NOT NULL,
        OrderNumber int NOT NULL,
        CONSTRAINT FK_AssessmentQuestions_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.AssessmentOptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssessmentOptions (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AssessmentQuestionId int NOT NULL,
        Text nvarchar(500) NOT NULL,
        IsCorrect bit NOT NULL,
        OrderNumber int NOT NULL,
        CONSTRAINT FK_AssessmentOptions_AssessmentQuestions FOREIGN KEY (AssessmentQuestionId) REFERENCES dbo.AssessmentQuestions(Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.AssessmentAttempts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssessmentAttempts (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CourseId int NOT NULL,
        UserId nvarchar(450) NOT NULL,
        Type int NOT NULL,
        Score int NOT NULL,
        TotalQuestions int NOT NULL,
        Passed bit NOT NULL,
        CompletedAt datetime2 NOT NULL,
        CONSTRAINT FK_AssessmentAttempts_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AssessmentAttempts_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.AssessmentAnswers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AssessmentAnswers (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        AttemptId int NOT NULL,
        QuestionId int NOT NULL,
        SelectedOptionId int NULL,
        IsCorrect bit NOT NULL,
        CONSTRAINT FK_AssessmentAnswers_Attempts FOREIGN KEY (AttemptId) REFERENCES dbo.AssessmentAttempts(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AssessmentAnswers_Questions FOREIGN KEY (QuestionId) REFERENCES dbo.AssessmentQuestions(Id),
        CONSTRAINT FK_AssessmentAnswers_Options FOREIGN KEY (SelectedOptionId) REFERENCES dbo.AssessmentOptions(Id)
    );
END;

IF OBJECT_ID('dbo.CourseCertificates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CourseCertificates (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CourseId int NOT NULL,
        UserId nvarchar(450) NOT NULL,
        CertificateNumber nvarchar(40) NOT NULL,
        IssuedAt datetime2 NOT NULL,
        CONSTRAINT FK_CourseCertificates_Courses FOREIGN KEY (CourseId) REFERENCES dbo.Courses(Id) ON DELETE CASCADE,
        CONSTRAINT FK_CourseCertificates_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_CourseCertificates_UserId_CourseId ON dbo.CourseCertificates(UserId, CourseId);
    CREATE UNIQUE INDEX IX_CourseCertificates_CertificateNumber ON dbo.CourseCertificates(CertificateNumber);
END;");
    }
}
