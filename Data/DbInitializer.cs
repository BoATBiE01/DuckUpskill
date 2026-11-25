using DuckUpskill.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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

        // Keep admin account
        await SeedUserAsync(userManager, "admin@duckupskill.com", "Admin@Duck123!", "Duck Master (Administrator)", "Admin");

        // Remove student demo account if present
        await DeleteUserIfExistsAsync(context, userManager, "student@duckupskill.com");

        // Seed & Update all 28 courses with curated YouTube video embeds, lesson content, and Unsplash images
        await CourseContentSeeder.SeedOrUpdateContentAsync(context);

        // Seed & Update authentic, curriculum-specific Pre-test & Post-test questions with realistic choices
        await CourseAssessmentSeeder.SeedOrUpdateAssessmentsAsync(context);

        // Seed initial sample support tickets if empty
        await SeedSampleSupportTicketsAsync(context);

        Console.WriteLine("[DuckUpskill] ฐานข้อมูลและข้อมูลพื้นฐานพร้อมใช้งาน");
    }

    private static async Task DeleteUserIfExistsAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null) return;

        var attempts = await context.AssessmentAttempts.Where(a => a.UserId == user.Id).ToListAsync();
        var attemptIds = attempts.Select(a => a.Id).ToList();
        if (attemptIds.Count > 0)
        {
            var answers = await context.AssessmentAnswers.Where(a => attemptIds.Contains(a.AttemptId)).ToListAsync();
            context.AssessmentAnswers.RemoveRange(answers);
            context.AssessmentAttempts.RemoveRange(attempts);
        }

        context.CourseCertificates.RemoveRange(await context.CourseCertificates.Where(c => c.UserId == user.Id).ToListAsync());
        context.LessonVideoWatches.RemoveRange(await context.LessonVideoWatches.Where(w => w.UserId == user.Id).ToListAsync());
        context.LessonCompletions.RemoveRange(await context.LessonCompletions.Where(c => c.UserId == user.Id).ToListAsync());
        context.Enrollments.RemoveRange(await context.Enrollments.Where(e => e.UserId == user.Id).ToListAsync());
        context.Orders.RemoveRange(await context.Orders.Where(o => o.UserId == user.Id).ToListAsync());

        var userTickets = await context.SupportTickets.Where(t => t.UserId == user.Id).ToListAsync();
        foreach (var t in userTickets) { t.UserId = null; }
        var userMessages = await context.SupportMessages.Where(m => m.SenderUserId == user.Id).ToListAsync();
        foreach (var m in userMessages) { m.SenderUserId = null; }

        await context.SaveChangesAsync();

        await userManager.DeleteAsync(user);
        Console.WriteLine($"[DuckUpskill] ลบบัญชีเดโม {email} เรียบร้อยแล้ว");
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
END;

IF OBJECT_ID('dbo.SupportTickets', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTickets (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId nvarchar(450) NULL,
        Name nvarchar(100) NOT NULL,
        Email nvarchar(256) NOT NULL,
        Topic nvarchar(150) NOT NULL,
        Status int NOT NULL CONSTRAINT DF_SupportTickets_Status DEFAULT 0,
        CreatedAt datetime2 NOT NULL,
        UpdatedAt datetime2 NOT NULL,
        CONSTRAINT FK_SupportTickets_Users FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
    );
END;

IF OBJECT_ID('dbo.SupportMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportMessages (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TicketId int NOT NULL,
        SenderUserId nvarchar(450) NULL,
        SenderName nvarchar(100) NOT NULL,
        IsAdmin bit NOT NULL CONSTRAINT DF_SupportMessages_IsAdmin DEFAULT 0,
        Message nvarchar(max) NOT NULL,
        SentAt datetime2 NOT NULL,
        CONSTRAINT FK_SupportMessages_Tickets FOREIGN KEY (TicketId) REFERENCES dbo.SupportTickets(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SupportMessages_Users FOREIGN KEY (SenderUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL
    );
END;");
    }

    private static async Task SeedSampleSupportTicketsAsync(ApplicationDbContext context)
    {
        if (await context.SupportTickets.AnyAsync()) return;

        var sampleTicket = new SupportTicket
        {
            Name = "สมชาย ใจดี",
            Email = "somchai@example.com",
            Topic = "สอบถามเกี่ยวกับเนื้อหาและใบ Certificate คอร์ส ASP.NET Core",
            Status = SupportTicketStatus.Replied,
            CreatedAt = DateTime.UtcNow.AddHours(-5),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
            Messages = new List<SupportMessage>
            {
                new SupportMessage
                {
                    SenderName = "สมชาย ใจดี",
                    IsAdmin = false,
                    Message = "สวัสดีครับทีมงาน อยากทราบว่าหากเรียนคอร์ส ASP.NET Core จบแล้วต้องทำคะแนน Post-test ได้กี่เปอร์เซ็นต์ถึงจะได้ Certificate ครับ?",
                    SentAt = DateTime.UtcNow.AddHours(-5)
                },
                new SupportMessage
                {
                    SenderName = "Duck Support (Admin)",
                    IsAdmin = true,
                    Message = "สวัสดีครับคุณสมชาย 🦆 สำหรับการรับ Certificate ผู้เรียนจำเป็นต้องเรียนบทเรียนให้ครบ 100% และทำแบบทดสอบ Post-test ให้ได้คะแนน 80% ขึ้นไปครับ หากมีข้อสงสัยเพิ่มเติมสอบถามได้เลยนะครับ!",
                    SentAt = DateTime.UtcNow.AddHours(-1)
                }
            }
        };

        var pendingTicket = new SupportTicket
        {
            Name = "นฤมล สนใจเรียน",
            Email = "narumon@example.com",
            Topic = "สอบถามการออกใบกำกับภาษีสำหรับคอร์สพรีเมียม",
            Status = SupportTicketStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-40),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-40),
            Messages = new List<SupportMessage>
            {
                new SupportMessage
                {
                    SenderName = "นฤมล สนใจเรียน",
                    IsAdmin = false,
                    Message = "สวัสดีค่ะ สนใจลงทะเบียนคอร์ส Python และ Power BI ทางระบบสามารถขอใบเสร็จหรือใบกำกับภาษีในนามบริษัทได้ไหมคะ?",
                    SentAt = DateTime.UtcNow.AddMinutes(-40)
                }
            }
        };

        context.SupportTickets.AddRange(sampleTicket, pendingTicket);
        await context.SaveChangesAsync();
        Console.WriteLine("[DuckUpskill] เพิ่มข้อมูลตัวอย่าง Support Tickets เริ่มต้นเรียบร้อย");
    }
}
