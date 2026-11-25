using DuckUpskill.Models;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Data;

public static class CourseContentSeeder
{
    public record CourseSeedInfo(
        string Slug,
        string Title,
        string Category,
        string ThumbnailUrl,
        string ShortDescription,
        string Description,
        (string Title, string Summary, string VideoUrl)[] Lessons
    );

    public static async Task SeedOrUpdateContentAsync(ApplicationDbContext context)
    {
        var coursesData = GetCuratedCourses();

        foreach (var info in coursesData)
        {
            var course = await context.Courses
                .Include(c => c.Lessons)
                .FirstOrDefaultAsync(c => c.Slug == info.Slug);

            if (course == null) continue;

            if (string.IsNullOrWhiteSpace(course.ThumbnailUrl))
            {
                course.ThumbnailUrl = info.ThumbnailUrl;
            }
            if (!string.IsNullOrWhiteSpace(info.ShortDescription))
                course.ShortDescription = info.ShortDescription;
            if (!string.IsNullOrWhiteSpace(info.Description))
                course.Description = info.Description;

            var existingLessons = course.Lessons.OrderBy(l => l.OrderNumber).ToList();

            for (int i = 0; i < info.Lessons.Length; i++)
            {
                var lessonData = info.Lessons[i];
                var orderNum = i + 1;

                if (i < existingLessons.Count)
                {
                    var existing = existingLessons[i];
                    existing.Title = $"{orderNum}. {lessonData.Title}";
                    existing.VideoUrl = lessonData.VideoUrl;
                    existing.Content = lessonData.Summary;
                    existing.IsPreview = (i == 0);
                }
                else
                {
                    var newLesson = new Lesson
                    {
                        CourseId = course.Id,
                        OrderNumber = orderNum,
                        Title = $"{orderNum}. {lessonData.Title}",
                        VideoUrl = lessonData.VideoUrl,
                        Content = lessonData.Summary,
                        DurationMinutes = 20 + (i * 5),
                        IsPreview = (i == 0)
                    };
                    context.Lessons.Add(newLesson);
                }
            }
        }

        await context.SaveChangesAsync();
        Console.WriteLine("[DuckUpskill] อัปเดตคลิปวิดีโอแบบไม่ซ้ำกันในแต่ละบทเรียนครบทุกคอร์สเรียบร้อย");
    }

    public static List<CourseSeedInfo> GetCuratedCourses()
    {
        return new List<CourseSeedInfo>
        {
            // 1. Web Dev (HTML, CSS, JS)
            new(
                "intro-html-css-javascript",
                "เริ่มต้นเขียนเว็บด้วย HTML, CSS & JavaScript ฉบับเป็ดน้อย 🦆",
                "Web Development",
                "https://images.unsplash.com/photo-1593720213428-28a5b9e94613?auto=format&fit=crop&w=800&q=80",
                "ปูพื้นฐานการเขียนเว็บตั้งแต่ศูนย์ เข้าใจโครงสร้าง HTML จัดสไตล์ด้วย CSS และเพิ่มลูกเล่นด้วย JavaScript",
                "คอร์สเรียนนี้ออกแบบมาเพื่อผู้เริ่มต้นโดยเฉพาะ คุณจะได้เรียนรู้การสร้างเว็บไซต์ตั้งแต่หน้าแรก การจัด Layout ด้วย Flexbox และการเขียน JavaScript โต้ตอบกับผู้ใช้งานจริง",
                new[]
                {
                    ("ยินดีต้อนรับสู่ DuckUpskill & แนะนำเครื่องมือ VS Code", "แนะนำการเตรียมสภาพแวดล้อม ติดตั้ง Visual Studio Code, Live Server extension และทำความเข้าใจว่าเว็บไซต์ทำงานอย่างไรบนเว็บเบราว์เซอร์", "https://www.youtube.com/embed/VqCgcpAypFQ"),
                    ("โครงสร้างพื้นฐาน HTML5 และ Semantic Tags", "เรียนรู้แท็กหลัก: <!DOCTYPE>, <html>, <head>, <body>, <h1>-<h6>, <p>, <a>, <img> และ Semantic HTML เช่น <header>, <nav>, <section>, <footer>", "https://www.youtube.com/embed/kUMe1FH4CHE"),
                    ("ตกแต่งหน้าตาเว็บด้วย CSS3 และ CSS Flexbox", "เข้าใจ CSS Selectors, Box Model (Margin, Border, Padding) และจัดเลย์เอาต์หน้าเว็บให้ responsive ยืดหยุ่นด้วย Flexbox", "https://www.youtube.com/embed/OXGznpKZ_sA"),
                    ("เพิ่มชีวิตชีวาให้เว็บไซต์ด้วย JavaScript DOM Manipulation", "พื้นฐานตัวแปร (let, const), Event Listeners, การดักจับคลิก และการแก้ไข DOM แบบเรียลไทม์", "https://www.youtube.com/embed/PkZNo7MFNFg"),
                    ("เวิร์กช็อปสร้างหน้า Landing Page ของตัวเอง", "นำความรู้ HTML, CSS และ JavaScript มาประกอบร่างเป็นหน้าเว็บ Landing Page สวยงามพร้อมเผยแพร่", "https://www.youtube.com/embed/zfQollXA_n4")
                }
            ),

            // 2. Fullstack ASP.NET Core MVC
            new(
                "fullstack-aspnet-core-mvc",
                "Full-Stack Web Development ด้วย ASP.NET Core & Modern UI",
                "Backend & Full-Stack",
                "https://images.unsplash.com/photo-1555066931-4365d14bab8c?auto=format&fit=crop&w=800&q=80",
                "สร้างเว็บแอปพลิเคชันระดับมืออาชีพด้วย ASP.NET Core MVC, Entity Framework Core, SQL Server และระบบรักษาความปลอดภัย",
                "ก้าวสู่การเป็น Full-Stack Developer ด้วย ASP.NET Core ประสิทธิภาพสูง เรียนรู้สถาปัตยกรรม MVC, การออกแบบ Database ด้วย EF Core, Authentication & Authorization และการทำระบบ CRUD ครบวงจร",
                new[]
                {
                    ("ภาพรวมสถาปัตยกรรม ASP.NET Core และ MVC Pattern", "ทำความเข้าใจวงจร Request-Response, Routing, Controllers, Views, Models และ Dependency Injection Container", "https://www.youtube.com/embed/C5cnZ-gZy2I"),
                    ("Entity Framework Core และการเชื่อมต่อ SQL Server", "การสร้าง DbContext, Data Modeling, Migrations และการจัดการ Relationships ใน SQL Server", "https://www.youtube.com/embed/d5xoxkP0120"),
                    ("การทำระบบ Identity, Role และสิทธิ์การใช้งาน (Admin & Member)", "ความปลอดภัยระดับ Enterprise ด้วย ASP.NET Core Identity, Password Hashing และ Role-based Authorization", "https://www.youtube.com/embed/0_m3N4p6v1E"),
                    ("ทำระบบจัดการคอร์สและการอัปโหลดไฟล์ (Admin CRUD)", "สร้างระบบหลังบ้าน Admin Dashboard สำหรับเพิ่ม แก้ไข ลบ คอร์สเรียนและบทเรียนพร้อม Validation", "https://www.youtube.com/embed/hZ16F_mJfls"),
                    ("ระบบสั่งซื้อ ชำระเงิน และการตรวจสอบสิทธิ์การเข้าถึงคอร์ส", "ออกแบบ Order Flow, Mock Payment Gateway และการปลดล็อกบทเรียนอัตโนมัติเมื่อชำระเงิน", "https://www.youtube.com/embed/PzK4ZXSk0k8")
                }
            ),

            // 3. Git & GitHub
            new(
                "git-github-fundamentals",
                "Git & GitHub พื้นฐานฉบับทำงานร่วมกันเป็นทีม",
                "DevOps & Tools",
                "https://images.unsplash.com/photo-1618401471353-b98afee0b2eb?auto=format&fit=crop&w=800&q=80",
                "เครื่องมือที่โปรแกรมเมอร์ทุกคนต้องเป็น บริหารเวอร์ชันโค้ด แตก Branch แก้ไข Conflict และทำงานร่วมกันในทีม",
                "เรียนรู้ Git command-line สำคัญ: clone, commit, push, pull, branch, merge, rebase พร้อมการทำ Pull Request และ Code Review บน GitHub",
                new[]
                {
                    ("Git คืออะไร ทำไมโลกนี้ขาด Git ไม่ได้", "แนวคิด Version Control System, ความแตกต่างระหว่าง Git และ GitHub, และสถานะทั้ง 3 ของ Git (Working Directory, Staging, Repository)", "https://www.youtube.com/embed/RGOj5yH7evk"),
                    ("คำสั่งพื้นฐาน: add, commit, status, log", "การบันทึกประวัติการเปลี่ยนแปลงโค้ด การเขียน Commit Message ที่ดี และการย้อนดูประวัติด้วย git log", "https://www.youtube.com/embed/8JJ101D3knE"),
                    ("การใช้งาน Branch, Merge และรับมือ Merge Conflicts", "การแตกกิ่งเพื่อพัฒนาฟีเจอร์ใหม่ การรวมโค้ดด้วย git merge และเทคนิคการแก้ไขข้อขัดแย้งของโค้ดอย่างมั่นใจ", "https://www.youtube.com/embed/oPpnCh7InLY")
                }
            ),

            // 4. AI & LLM .NET
            new(
                "building-ai-apps-dotnet-llm",
                "สร้าง AI Application ด้วย C# .NET และ LLM Integration",
                "AI & Machine Learning",
                "https://images.unsplash.com/photo-1677442136019-21780efad99a?auto=format&fit=crop&w=800&q=80",
                "นำพลังของโมเดล AI ขนาดใหญ่มาประยุกต์ใช้ในแอปพลิเคชัน .NET เชื่อมต่อ API ทำ Chatbot และระบบ RAG ค้นหาเอกสาร",
                "คอร์สสำหรับนักพัฒนา .NET ที่ต้องการยกระดับสู่ยุค Generative AI สอนเทคนิค Prompt Engineering ในโค้ด C#, การเรียกใช้งาน AI APIs, Streaming Response และ Function Calling",
                new[]
                {
                    ("ปูพื้นฐาน Generative AI & Large Language Models สำหรับ Dev", "เข้าใจ Tokens, Temperature, Context Window, Embeddings และสถาปัตยกรรม Transformer", "https://www.youtube.com/embed/9-Jl0dxWQs8"),
                    ("เชื่อมต่อ API ด้วย C# HttpClient และการทำ Streaming Response", "การส่งคำขอแบบ Streaming ข้อความทีละคำใน ASP.NET Core และการจัดการข้อผิดพลาด Network", "https://www.youtube.com/embed/2t3_0_q_wZg"),
                    ("การทำ Function Calling / Tool Use ให้ AI เชื่อมต่อฐานข้อมูล", "สร้าง AI Agent ที่สามารถเรียกใช้งานฟังก์ชันใน C# เพื่อค้นหาข้อมูลใน SQL Server และสรุปคำตอบให้ผู้ใช้", "https://www.youtube.com/embed/0lOSvOoF2WE")
                }
            ),

            // 5. SQL Server Database Design
            new(
                "sql-server-database-design",
                "SQL Server & Database Design สำหรับ Developer สมัยใหม่",
                "Database",
                "https://images.unsplash.com/photo-1544383835-bda2bc66a55d?auto=format&fit=crop&w=800&q=80",
                "ออกแบบฐานข้อมูลเชิงสัมพันธ์ ทำ Normalization เขียน Query วิเคราะห์ความเร็ว และทำ Index Optimization",
                "เจาะลึก SQL Server ตั้งแต่การออกแบบ ER Diagram, Primary/Foreign Keys, Constraints, Normalization ไปจนถึงการเขียนคำสั่ง SELECT, JOIN และ GROUP BY",
                new[]
                {
                    ("การติดตั้ง SQL Server & SQL Server Management Studio (SSMS)", "การติดตั้ง LocalDB และเครื่องมือ SSMS การเชื่อมต่อฐานข้อมูล และการสร้าง Database แรก", "https://www.youtube.com/embed/7S_tz1z_5bA"),
                    ("พื้นฐาน DDL, DML และคำสั่ง SELECT, JOIN, GROUP BY", "การสร้างตาราง (CREATE TABLE), การจัดการข้อมูล (INSERT, UPDATE, DELETE) และการสืบค้นขั้นสูงด้วย JOIN และ Aggregation", "https://www.youtube.com/embed/HXV3zeQKqGY")
                }
            ),

            // 6. Tech Soft Skills
            new(
                "tech-soft-skills-interview",
                "ทักษะ Soft Skills และการเตรียมสัมภาษณ์งานสาย Tech ฉบับเป็ดบินได้ 🚀",
                "Career & Soft Skills",
                "https://images.unsplash.com/photo-1522071820081-009f0129c71c?auto=format&fit=crop&w=800&q=80",
                "เทคนิคเขียน Resume สายโปรแกรมเมอร์ การสื่อสารในทีม Agile และวิธีตอบคำถามสัมภาษณ์เชิงพฤติกรรม (STAR Method)",
                "ขัดเกลาการสื่อสาร การทำงานเป็นทีม และการเตรียมตัวสัมภาษณ์งานเพื่อคว้าตำแหน่งงานในฝันและต่อรองผลตอบแทนอย่างมืออาชีพ",
                new[]
                {
                    ("การเขียน Tech Resume & Portfolio ที่ดึงดูดสายตา HR ภายใน 6 วินาที", "โครงสร้างเรซูเม่ที่กระชับ การเน้นย้ำทักษะที่เกี่ยวข้อง และการจัดทำ Portfolio ผลงานจริง", "https://www.youtube.com/embed/bPVaOlJ6ln0"),
                    ("พิชิตคำถามสัมภาษณ์ Behavior ด้วยเทคนิค STAR Method", "การเล่าเรื่องความสำเร็จด้วย Situation, Task, Action, Result และการรับมือคำถามกดดันในการสัมภาษณ์", "https://www.youtube.com/embed/uG36dD8_mK4")
                }
            ),

            // 8. Python Beginner Projects
            new(
                "python-beginner-projects",
                "Python สำหรับผู้เริ่มต้น: จากศูนย์สู่โปรเจกต์แรก",
                "Programming",
                "https://images.unsplash.com/photo-1526374965328-7f61d4dc18c5?auto=format&fit=crop&w=800&q=80",
                "เริ่มเขียน Python ตั้งแต่ตัวแปร เงื่อนไข ลูป และฟังก์ชัน พร้อมสร้างโปรเจกต์เล็ก ๆ",
                "ฝึกพื้นฐาน Python ด้วยตัวอย่างและแบบฝึกหัดทีละขั้น เหมาะอย่างยิ่งสำหรับผู้ที่ต้องการเริ่มต้นเขียนโปรแกรมภาษาแรก",
                new[]
                {
                    ("ติดตั้ง Python และเริ่มเขียนโปรแกรมแรก", "การติดตั้ง Python, VS Code และการใช้คำสั่ง print(), input() และตัวแปรพื้นฐาน", "https://www.youtube.com/embed/rfscVS0vtbw"),
                    ("ชนิดข้อมูล ตัวแปร และการคำนวณ", "ทำความเข้าใจ int, float, str, bool, การแปลงชนิดข้อมูล (Type Casting) และตัวดำเนินการทางคณิตศาสตร์", "https://www.youtube.com/embed/kqtD5dpn9C8"),
                    ("การควบคุมเงื่อนไขและการวนซ้ำ (Loops)", "การใช้ if-elif-else เพื่อตัดสินใจ และการวนซ้ำด้วย for loop และ while loop", "https://www.youtube.com/embed/6iF8Xb7Z3wQ"),
                    ("ฟังก์ชันและการจัดการข้อผิดพลาด (Exception Handling)", "การประกาศฟังก์ชันด้วย def, การส่งพารามิเตอร์, การ return ค่า และการดักจับ error ด้วย try-except", "https://www.youtube.com/embed/9Os0o3wzS_I"),
                    ("เวิร์กช็อปสร้างเกมทายตัวเลข (Number Guessing Game)", "รวมความรู้ทั้งหมดมาเขียนโปรแกรมเกมทายตัวเลขแบบสุ่มที่มีการนับคะแนนและระบบคำใบ้", "https://www.youtube.com/embed/d3WbE9Dfgxk")
                }
            ),

            // 9. C# & .NET Foundations
            new(
                "csharp-dotnet-foundations",
                "C# และ .NET พื้นฐานสำหรับนักพัฒนา",
                "Programming",
                "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?auto=format&fit=crop&w=800&q=80",
                "เรียนรู้ C# ตั้งแต่พื้นฐาน OOP, Collections และ LINQ พร้อมทดลองทำแอป Console",
                "คอร์สปูพื้นฐาน C# และ .NET สำหรับผู้เริ่มต้นที่ต้องการต่อยอดสู่ ASP.NET Core, Game Development หรือ Enterprise Software",
                new[]
                {
                    ("เตรียมเครื่องมือและโครงสร้างโปรเจกต์ .NET", "รู้จัก .NET SDK, คำสั่ง dotnet CLI (new, build, run) และโครงสร้างไฟล์ .csproj", "https://www.youtube.com/embed/GhQdlIFylQ8"),
                    ("ตัวแปร ชนิดข้อมูล และเมธอดใน C#", "การประกาศตัวแปรอย่างเข้มงวด (Strongly Typed), เมธอด, พารามิเตอร์ และการคืนค่า", "https://www.youtube.com/embed/gfkTfcpWqAY"),
                    ("คลาส ออบเจ็กต์ และหลักการ OOP", "ทำความเข้าใจ Encapsulation, Inheritance, Polymorphism และการสร้าง Constructor", "https://www.youtube.com/embed/qiP6Q0v6_yM"),
                    ("Collections และ LINQ เบื้องต้น", "การใช้งาน List<T>, Dictionary<TKey, TValue> และการเขียน Query กรองข้อมูลด้วย LINQ", "https://www.youtube.com/embed/tM0f1Wv9j7o"),
                    ("สร้างแอปพลิเคชัน Console สำหรับจัดการรายการงาน", "เวิร์กช็อปสร้างโปรแกรม Todo List จัดเก็บข้อมูลและจัดการสถานะงานในรูปแบบ OOP", "https://www.youtube.com/embed/1hPgQc8F9E4")
                }
            ),

            // 10. React
            new(
                "react-component-web",
                "React สร้างเว็บแบบ Component",
                "Frontend",
                "https://images.unsplash.com/photo-1633356122544-f134324a6cee?auto=format&fit=crop&w=800&q=80",
                "เข้าใจ Component, Props, State, Hooks และการเรียก API ด้วย React",
                "สร้างส่วนติดต่อผู้ใช้แบบ Component ที่ทันสมัยด้วย React ไลบรารียอดนิยมอันดับ 1 ของโลก",
                new[]
                {
                    ("เริ่มต้นโปรเจกต์ React ด้วย Vite และทำความเข้าใจ JSX", "การติดตั้งสภาพแวดล้อมด้วย Vite, โครงสร้างไฟล์ และไวยากรณ์ JSX ที่รวม HTML เข้ากับ JavaScript", "https://www.youtube.com/embed/bMknfKXIFA8"),
                    ("การสร้าง Component, Props และการแสดงผลข้อมูลแบบไดนามิก", "การแบ่งหน้าจอเป็นชิ้นส่วนเล็กๆ และการส่งผ่านข้อมูลด้วย Props ระหว่าง Component แม่และลูก", "https://www.youtube.com/embed/Rh3tobg7hEo"),
                    ("การจัดการ State ด้วย useState และการดักจับ Event", "การเก็บสถานะใน Component, ฟังก์ชัน setState, และการตอบสนองต่อการคลิกหรือพิมพ์ของผู้ใช้", "https://www.youtube.com/embed/O6P86uwfdR0"),
                    ("การใช้ useEffect เพื่อเรียกใช้งาน REST API ภายนอก", "Side Effects ใน React, การยิงคำขอ HTTP เพื่อดึงข้อมูล JSON มาแสดงผล และการจัดการ Loading State", "https://www.youtube.com/embed/0ZJgIjIuY7U"),
                    ("เวิร์กช็อปสร้างหน้า Dashboard แสดงข้อมูลผู้ใช้งาน", "นำทุกความรู้มารวมกันเพื่อสร้าง Dashboard ที่ค้นหาและกรองข้อมูลได้แบบเรียลไทม์", "https://www.youtube.com/embed/w7ejDZ8SWv8")
                }
            ),

            // 11. TypeScript
            new(
                "typescript-for-javascript",
                "TypeScript สำหรับ JavaScript Developer",
                "Frontend",
                "https://images.unsplash.com/photo-1516116211227-bbc790d970b8?auto=format&fit=crop&w=800&q=80",
                "เพิ่มความมั่นใจให้โค้ดด้วย Type, Interface, Generics และการตั้งค่า TypeScript",
                "เปลี่ยนจาก JavaScript สู่ TypeScript เพื่อลดข้อผิดพลาดในระบบและเพิ่มประสิทธิภาพการทำงานร่วมกันในทีมขนาดใหญ่",
                new[]
                {
                    ("ติดตั้ง TypeScript และการตั้งค่า tsconfig.json", "การแปลงโค้ด TS เป็น JS, คอมไพเลอร์ tsc และการคอนฟิกตัวเลือกสำคัญใน tsconfig", "https://www.youtube.com/embed/30LWjhZzg50"),
                    ("Primitive Types, Arrays, Tuples และ Any/Unknown", "การระบุประเภทข้อมูลพื้นฐาน และความแตกต่างที่ปลอดภัยระหว่าง unknown กับ any", "https://www.youtube.com/embed/BwuLxPH8IDs"),
                    ("Interfaces vs Type Aliases และการทำ Union/Intersection", "การกำหนดโครงสร้างของ Object, การสืบทอด Interface และการรวม Type ด้วย | และ &", "https://www.youtube.com/embed/bcZhkC0i_60"),
                    ("Generics ในฟังก์ชันและคลาสเพื่อการนำกลับมาใช้ซ้ำ", "การเขียนฟังก์ชันที่ยืดหยุ่นรองรับทุกชนิดข้อมูลโดยไม่สูญเสียความปลอดภัยของ Type", "https://www.youtube.com/embed/nViEqpgwxHE"),
                    ("การ Migrate โปรเจกต์ JavaScript เดิมให้เป็น TypeScript", "แนวทางปฏิบัติทีละขั้นในการเปลี่ยนไฟล์ .js ให้เป็น .ts พร้อมแก้ Error อย่างถูกต้อง", "https://www.youtube.com/embed/F2JCjVSZlG0")
                }
            ),

            // 12. UX/UI Fundamentals
            new(
                "ux-ui-design-fundamentals",
                "ออกแบบ UX/UI ให้ใช้งานง่าย",
                "Design",
                "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?auto=format&fit=crop&w=800&q=80",
                "ฝึกทำความเข้าใจผู้ใช้ วางโครงหน้าจอ และออกแบบต้นแบบที่ทดสอบได้",
                "เรียนรู้กระบวนการออกแบบประสบการณ์ผู้ใช้ตั้งแต่โจทย์ทางธุรกิจจนถึง Prototype ที่ตอบโจทย์การใช้งานจริง",
                new[]
                {
                    ("ความแตกต่างระหว่าง UX และ UI และกระบวนการคิดเชิงออกแบบ", "ทำความเข้าใจความรู้สึกของผู้ใช้ (UX) ควบคู่กับความสวยงามของหน้าจอ (UI) และ Design Thinking", "https://www.youtube.com/embed/c9Wg6Cb_YlU"),
                    ("การทำ User Research และการสร้าง Persona", "เทคนิคการสัมภาษณ์ผู้ใช้เพื่อหา Pain Points และการสังเคราะห์เป็น Persona ตัวแทนผู้ใช้งาน", "https://www.youtube.com/embed/bAco_eF4x_M"),
                    ("การวาง User Flow และ Information Architecture (IA)", "การออกแบบเส้นทางการใช้งานตั้งแต่ก้าวแรกจนจบภารกิจ และการจัดหมวดหมู่เมนูในระบบ", "https://www.youtube.com/embed/68w2VwalD5w"),
                    ("การสร้าง Wireframe และการทำ Usability Testing", "การสเก็ตช์ภาพโครงร่างหน้าจอแบบ Low-fidelity และการนำไปทดสอบกับผู้ใช้เพื่อรับฟังข้อเสนอแนะ", "https://www.youtube.com/embed/qpH7-KFWZRI")
                }
            ),

            // 13. Figma
            new(
                "figma-ui-prototyping",
                "Figma ออกแบบหน้าจอและ Prototype",
                "Design",
                "https://images.unsplash.com/photo-1609921212029-bb5a28e60960?auto=format&fit=crop&w=800&q=80",
                "ใช้ Figma สร้างหน้าจอ Component, Auto Layout และ Prototype สำหรับส่งต่องาน",
                "ฝึกทำงานออกแบบ UI ตั้งแต่ไฟล์เริ่มต้นจนถึงต้นแบบคลิกได้ พร้อมการจัดทำ Design System สำหรับทีม",
                new[]
                {
                    ("แนะนำพื้นที่การทำงานและเครื่องมือพื้นฐานใน Figma", "การนำทางใน Canvas, การสร้าง Frames, Shapes, Text และการใช้ Color Styles", "https://www.youtube.com/embed/jwCmIBJ8J5A"),
                    ("การใช้งาน Frame, Grid และการจัดวาง Typography", "การวาง Layout Grid สำหรับ Desktop และ Mobile และการกำหนดสเกลของตัวอักษร", "https://www.youtube.com/embed/cx_Vz_iW_Wk"),
                    ("การสร้าง Components, Variants และ Auto Layout ขั้นสูง", "หัวใจสำคัญของการออกแบบยุคใหม่: การทำปุ่มที่ปรับขยายอัตโนมัติตามเนื้อหา", "https://www.youtube.com/embed/NrKX46DzkGQ"),
                    ("การทำ Interactive Prototype และ Smart Animate", "การเชื่อมโยงหน้าจอ การสร้าง Transitions ที่ลื่นไหล และการทดสอบบน Figma Mirror", "https://www.youtube.com/embed/4W4IssT-sQ8"),
                    ("การเตรียม Design System และ Hand-off สำหรับทีม Developer", "การจัดหมวดหมู่ Asset, การส่งออกภาพ SVG/PNG และการใช้ Inspect Tool สำหรับโปรแกรมเมอร์", "https://www.youtube.com/embed/el_m6_9bB7k")
                }
            ),

            // 14. Excel Workplace Skills
            new(
                "excel-workplace-skills",
                "Excel ใช้งานจริง: สูตร ตาราง และ PivotTable",
                "Productivity",
                "https://images.unsplash.com/photo-1551288049-bebda4e38f71?auto=format&fit=crop&w=800&q=80",
                "จัดการข้อมูลด้วยสูตรที่ใช้บ่อย ตาราง PivotTable และกราฟพื้นฐาน",
                "เพิ่มความคล่องตัวในการทำงานสำนักงานด้วยเครื่องมือ Excel ที่ใช้จริงในชีวิตประจำวัน",
                new[]
                {
                    ("การจัดรูปแบบตารางข้อมูลและการใช้ Data Validation", "การเปลี่ยนช่วงข้อมูลเป็น Excel Table และการควบคุมการกรอกข้อมูลด้วย Drop-down list", "https://www.youtube.com/embed/Vl0H-qTclOg"),
                    ("สูตรคำนวณพื้นฐานและการใช้ฟังก์ชัน IF, SUMIFS, COUNTIFS", "เทคนิคการคำนวณตามเงื่อนไขเพื่อหาผลรวมและนับจำนวนรายการอัตโนมัติ", "https://www.youtube.com/embed/rwbho0CgEAE"),
                    ("การค้นหาและจับคู่ข้อมูลด้วย VLOOKUP และ XLOOKUP", "การดึงข้อมูลข้ามตารางอย่างแม่นยำ และข้อได้เปรียบของฟังก์ชัน XLOOKUP สมัยใหม่", "https://www.youtube.com/embed/2_H0hX1pBEE"),
                    ("การสร้าง PivotTable และ PivotChart สรุปผลยอดขาย", "การสรุปผลข้อมูลปริมาณมากใน 3 คลิก และการสร้าง Dashboard แบบสรุปภาพรวม", "https://www.youtube.com/embed/m0wI61ahfLc")
                }
            ),

            // 15. Power BI Dashboard
            new(
                "power-bi-dashboard",
                "Power BI สร้างรายงานและ Dashboard",
                "Data Analytics",
                "https://images.unsplash.com/photo-1460925895917-afdab827c52f?auto=format&fit=crop&w=800&q=80",
                "นำเข้าข้อมูล สร้าง Model เขียน Measure เบื้องต้น และออกแบบรายงาน",
                "เรียนรู้ขั้นตอนสร้างรายงานข้อมูลที่อ่านง่ายและนำไปใช้ตัดสินใจทางธุรกิจได้จริง",
                new[]
                {
                    ("ทำความรู้จัก Power BI Desktop และการนำเข้าข้อมูล (Power Query)", "การเชื่อมต่อแหล่งข้อมูล Excel/SQL และการแปลงข้อมูลให้อยู่ในรูปแบบที่พร้อมวิเคราะห์", "https://www.youtube.com/embed/TmhQCQr_DCA"),
                    ("การทำความสะอาดข้อมูลและการออกแบบ Data Modeling", "การสร้างความสัมพันธ์ (Star Schema, One-to-Many) ระหว่างตาราง Fact และ Dimension", "https://www.youtube.com/embed/AGrl-H87pRU"),
                    ("การเขียนสูตรคำนวณ DAX: Calculated Columns & Measures", "พื้นฐานสูตร DAX เช่น CALCULATE, SUM, RELATED สำหรับการวิเคราะห์เชิงลึก", "https://www.youtube.com/embed/yL1WnZg4nDg"),
                    ("การเลือก Visualizations ที่เหมาะสมและการจัดวาง Layout", "การเลือก Bar chart, Line chart, KPI card ให้สอดคล้องกับเรื่องราวที่ต้องการนำเสนอ", "https://www.youtube.com/embed/r25gS9eO49k"),
                    ("การตั้งค่า Drill-through, Slicers และการแชร์ Report", "เพิ่มฟังก์ชันการกรองข้อมูลแบบมีปฏิสัมพันธ์ และการนำเสนอต่อผู้บริหาร", "https://www.youtube.com/embed/e_5fO7f_64A")
                }
            ),

            // 16. SQL for Data Analysis
            new(
                "sql-for-data-analysis",
                "SQL สำหรับนักวิเคราะห์ข้อมูล",
                "Data Analytics",
                "https://images.unsplash.com/photo-1504868584819-f8e8b4b6d7e3?auto=format&fit=crop&w=800&q=80",
                "ฝึก SELECT, JOIN, Aggregation และ Window Function ด้วยโจทย์วิเคราะห์ข้อมูล",
                "ใช้ SQL ตอบคำถามทางธุรกิจจากชุดข้อมูลตัวอย่างขนาดใหญ่ได้อย่างคล่องแคล่ว",
                new[]
                {
                    ("การดึงข้อมูลและกรองเงื่อนไข: SELECT, WHERE, LIKE, IN", "พื้นฐานการดึงคอลัมน์ การกำหนดเงื่อนไขค้นหา และการกรองช่วงข้อมูล", "https://www.youtube.com/embed/7mz73uXD9DA"),
                    ("การรวมตารางข้อมูลด้วย INNER, LEFT, RIGHT และ FULL JOIN", "เทคนิคการรวมข้อมูลจากหลายตารางเพื่อสร้างมุมมองวิเคราะห์ที่สมบูรณ์", "https://www.youtube.com/embed/9yeOJ0ZMUYw"),
                    ("การสรุปผลสถิติ: GROUP BY, HAVING และ Aggregate Functions", "การจัดกลุ่มข้อมูลเพื่อหาค่าเฉลี่ย ผลรวม และนับจำนวนรายการตามประเภทสินค้า", "https://www.youtube.com/embed/0r_bA0U8_lA"),
                    ("การเขียน Subquery และ Common Table Expressions (CTE)", "การแบ่งคำสั่ง Query ที่ซับซ้อนออกเป็นขั้นตอนที่อ่านง่ายด้วยคำสั่ง WITH", "https://www.youtube.com/embed/K8v0O2U_n2k"),
                    ("การใช้ Window Functions: ROW_NUMBER, RANK, LEAD, LAG", "การจัดอันดับข้อมูลและการเปรียบเทียบค่าระหว่างแถวก่อนหน้าและถัดไป", "https://www.youtube.com/embed/Ww71knvhQ-s")
                }
            ),

            // 17. Cybersecurity Awareness
            new(
                "cybersecurity-awareness",
                "พื้นฐาน Cybersecurity สำหรับคนทำงาน",
                "Cybersecurity",
                "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?auto=format&fit=crop&w=800&q=80",
                "รู้จักภัยออนไลน์ รหัสผ่านปลอดภัย Phishing และแนวทางปกป้องข้อมูลส่วนตัว",
                "สร้างความตระหนักด้านความปลอดภัยไซเบอร์สำหรับการใช้งานประจำวันในองค์กร",
                new[]
                {
                    ("ภัยคุกคามทางไซเบอร์ในชีวิตประจำวัน: Malware, Ransomware, Phishing", "ทำความรู้จักรูปแบบการโจมตีทางไซเบอร์ที่พบบ่อย และผลกระทบต่อองค์กร", "https://www.youtube.com/embed/bPVaOlJ6ln0"),
                    ("การตั้งรหัสผ่านที่ปลอดภัยและการใช้ Multi-Factor Authentication (MFA)", "วิธีสร้างรหัสผ่านที่ยากต่อการคาดเดา และความสำคัญของการเปิดใช้งาน MFA 2 ชั้น", "https://www.youtube.com/embed/inWWhr5tnEA"),
                    ("ความปลอดภัยบนเครือข่าย Wi-Fi สาธารณะและภัย Social Engineering", "อันตรายจากการใช้อินเทอร์เน็ตสาธารณะ และจิตวิทยาหลอกลวงที่แฮกเกอร์นิยมใช้", "https://www.youtube.com/embed/lc7qugMAVHQ"),
                    ("การปกป้องข้อมูลส่วนบุคคล (PDPA) และสุขอนามัยทางดิจิทัล", "แนวทางการจัดการข้อมูลสำคัญ และมาตรการรักษาความปลอดภัยของอุปกรณ์ทำงาน", "https://www.youtube.com/embed/sdpxddDzXfE")
                }
            ),

            // 18. Cloud & Azure Fundamentals
            new(
                "cloud-azure-fundamentals",
                "พื้นฐาน Cloud Computing และ Azure",
                "Cloud & DevOps",
                "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=800&q=80",
                "รู้จัก Cloud, IaaS/PaaS/SaaS และทดลองแนวคิดการ Deploy บน Azure",
                "ปูพื้นฐาน Cloud Computing และการเลือกใช้บริการให้เหมาะกับงานสถาปัตยกรรมระบบ",
                new[]
                {
                    ("แนวคิด Cloud Computing: IaaS, PaaS, SaaS และ Deployment Models", "เข้าใจความแตกต่างระหว่าง On-premises และ Cloud พร้อมข้อได้เปรียบด้านความยืดหยุ่น", "https://www.youtube.com/embed/NKEFW2WJbcE"),
                    ("โครงสร้างพื้นฐาน Azure: Regions, Availability Zones, Resource Groups", "การกระจายศูนย์ข้อมูลเพื่อความพร้อมใช้งานสูง (High Availability) และ Disaster Recovery", "https://www.youtube.com/embed/10pbKA5m58E"),
                    ("บริการประมวลผลและการจัดเก็บข้อมูล: Azure App Service & Storage", "การเลือกใช้ Compute Service ให้เหมาะสมกับขนาดและชนิดของแอปพลิเคชัน", "https://www.youtube.com/embed/b0hX2iWc76M"),
                    ("ความปลอดภัยและการจัดการต้นทุน: Azure RBAC & Cost Management", "การควบคุมสิทธิ์การเข้าถึงทรัพยากร และการตั้งค่างบประมาณแจ้งเตือนค่าใช้จ่าย", "https://www.youtube.com/embed/8eW4T_2Z9aA"),
                    ("เวิร์กช็อป Deploy เว็บแอปพลิเคชันขึ้นสู่ Azure App Service", "การนำเว็บแอปพลิเคชันขึ้นสู่ Cloud จริงพร้อมเปิดใช้งาน Custom Domain และ SSL", "https://www.youtube.com/embed/3c-iBn73dDE")
                }
            ),

            // 19. Docker for Developers
            new(
                "docker-for-developers",
                "Docker สำหรับนักพัฒนา",
                "Cloud & DevOps",
                "https://images.unsplash.com/photo-1605745341112-85968b19335b?auto=format&fit=crop&w=800&q=80",
                "สร้าง Image, Container, Volume และใช้ Compose เพื่อจัดสภาพแวดล้อมแอป",
                "เข้าใจ Containerization และฝึกแพ็กแอปให้ทำงานเหมือนกันทุกเครื่องอย่างไร้ปัญหา",
                new[]
                {
                    ("Containerization คืออะไร และเปรียบเทียบกับ Virtual Machine", "สถาปัตยกรรมของ Container, ความเบาและรวดเร็วในการเริ่มต้นใช้งานเทียบกับ VM", "https://www.youtube.com/embed/fqMOX6JJhGo"),
                    ("การติดตั้ง Docker และคำสั่งจัดการ Containers เบื้องต้น", "คำสั่ง docker run, ps, stop, rm, logs และการจัดการ Port Forwarding", "https://www.youtube.com/embed/pTFZFxd4hOI"),
                    ("การเขียน Dockerfile เพื่อ Build Image แอปพลิเคชัน", "คำสั่ง FROM, WORKDIR, COPY, RUN, EXPOSE, CMD และเทคนิค Multi-stage builds", "https://www.youtube.com/embed/0qotVMX-J5s"),
                    ("การจัดการ Storage ด้วย Volumes และการเชื่อมต่อ Networks", "การเก็บรักษาข้อมูลไม่ให้สูญหายด้วย Named Volumes และการสื่อสารระหว่าง Container", "https://www.youtube.com/embed/Wh4P4qX_vWw"),
                    ("การใช้งาน Docker Compose สำหรับรัน Multi-container Apps", "การเขียนไฟล์ docker-compose.yml เพื่อรัน Web App คู่กับ Database ในคำสั่งเดียว", "https://www.youtube.com/embed/HG6yIjLapSA")
                }
            ),

            // 20. Software Testing & Unit Tests
            new(
                "software-testing-unit-tests",
                "การทดสอบซอฟต์แวร์และเขียน Unit Test",
                "Software Quality",
                "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=800&q=80",
                "เรียนรู้ Test Case, Boundary Testing, Unit Test และ Mocking เบื้องต้น",
                "สร้างความมั่นใจในการเปลี่ยนแปลงโค้ดด้วยการทดสอบอัตโนมัติที่แข็งแกร่ง",
                new[]
                {
                    ("แนวคิดการทดสอบซอฟต์แวร์และโครงสร้าง Test Pyramid", "ทำไมต้องเขียน Test, ความแตกต่างระหว่าง Unit Test, Integration Test และ E2E Test", "https://www.youtube.com/embed/Jv2ux4MB4vU"),
                    ("การเขียน Unit Test ด้วย xUnit และแนวคิด AAA (Arrange, Act, Assert)", "โครงสร้างมาตรฐานของการเขียนเคสทดสอบที่เข้าใจง่ายและบำรุงรักษาสะดวก", "https://www.youtube.com/embed/2_Hk0Q_uW_k"),
                    ("การทดสอบกรณีขอบเขต (Boundary Testing) และ Data-Driven Tests", "การทดสอบค่าสุดขีด ข้อมูลว่างเปล่า และการใช้ [Theory] ป้อนข้อมูลทดสอบหลายชุด", "https://www.youtube.com/embed/Wb8jG9_W5eA"),
                    ("การจำลอง Dependencies ด้วย Mocking Framework (Moq)", "การแยกส่วนที่ทดสอบออกจาก Database หรือ External API ด้วยการสร้าง Mock Objects", "https://www.youtube.com/embed/jZ1hN9_0_kM"),
                    ("การวัด Code Coverage และแนวปฏิบัติในการเขียน Test ให้ดูแลรักษาง่าย", "การวิเคราะห์เปอร์เซ็นต์ความครอบคลุมของการทดสอบ และการหลีกเลี่ยง Fragile Tests", "https://www.youtube.com/embed/k_0X_v8rW7U")
                }
            ),

            // 21. GitHub Actions CI Basics
            new(
                "github-actions-ci-basics",
                "การใช้ GitHub Actions ทำ CI เบื้องต้น",
                "Cloud & DevOps",
                "https://images.unsplash.com/photo-1556075798-4825dfaaf498?auto=format&fit=crop&w=800&q=80",
                "ตั้ง Workflow ให้ Build และทดสอบโปรเจกต์อัตโนมัติเมื่อ Push หรือเปิด Pull Request",
                "เริ่มใช้ CI เพื่อจับปัญหาก่อนรวมโค้ดเข้าสาขาหลักและเพิ่มคุณภาพงานของทีม",
                new[]
                {
                    ("ความเข้าใจเกี่ยวกับ CI/CD และการทำงานอัตโนมัติ", "วงจร Continuous Integration และประโยชน์ของการตรวจสอบโค้ดแบบอัตโนมัติ", "https://www.youtube.com/embed/dOxUroR57xs"),
                    ("โครงสร้างไฟล์ Workflow: Triggers, Jobs, Runners และ Steps", "การเขียนไฟล์ YAML กำหนดการทำงานในโฟลเดอร์ .github/workflows", "https://www.youtube.com/embed/R8_veQiYBjI"),
                    ("การตั้งค่า Automated Build & Unit Test เมื่อมี Pull Request", "การสั่งรัน dotnet restore, dotnet build และ dotnet test ทุกครั้งที่มีการเปิด PR", "https://www.youtube.com/embed/mFFXuXjVgkU"),
                    ("การจัดการ Secrets และ Environment Variables อย่างปลอดภัย", "การเก็บรหัสผ่านและ API Keys ใน GitHub Secrets เพื่อความปลอดภัยในการ Deploy", "https://www.youtube.com/embed/2Yl_vWbB8_M")
                }
            ),

            // 22. Agile & Scrum
            new(
                "agile-scrum-practical",
                "การวางแผนงานด้วย Agile และ Scrum",
                "Career & Soft Skills",
                "https://images.unsplash.com/photo-1531403009284-440f080d1e12?auto=format&fit=crop&w=800&q=80",
                "เข้าใจ Sprint, Backlog, User Story และกิจกรรม Scrum สำหรับทีมพัฒนา",
                "ทำความเข้าใจวิธีทำงานแบบ Agile พร้อมฝึกวางแผนรอบงานในสภาพแวดล้อมจริง",
                new[]
                {
                    ("ปรัชญา Agile และค่านิยม 4 ประการในการพัฒนาซอฟต์แวร์", "ความยืดหยุ่นในการตอบสนองต่อความเปลี่ยนแปลง และการส่งมอบซอฟต์แวร์ที่ใช้งานได้จริง", "https://www.youtube.com/embed/9TycLR0TqFA"),
                    ("โครงสร้างทีม Scrum และบทบาท: Product Owner, Scrum Master, Developers", "หน้าที่และความรับผิดชอบของแต่ละบทบาทเพื่อให้ทีมทำงานได้อย่างเต็มศักยภาพ", "https://www.youtube.com/embed/2Vt7Ik8Ubl8"),
                    ("วงจรการทำงาน: Sprint Planning, Daily Scrum, Review และ Retrospective", "พิธีกรรมทั้ง 4 ของ Scrum และการประเมินผลงานเพื่อปรับปรุงอย่างต่อเนื่อง", "https://www.youtube.com/embed/VFqtOC_hT6A"),
                    ("การเขียน User Stories, Story Points และการประมาณการงาน", "เทคนิคการแบ่งงานย่อย การกำหนดเกณฑ์การยอมรับ (Acceptance Criteria) และ Planning Poker", "https://www.youtube.com/embed/0xV1n4g7lU0")
                }
            ),

            // 23. Tech Communication
            new(
                "tech-communication-presentation",
                "การสื่อสารและนำเสนองานสำหรับสายเทค",
                "Career & Soft Skills",
                "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?auto=format&fit=crop&w=800&q=80",
                "ฝึกอธิบายเรื่องเทคนิคให้เข้าใจง่าย เขียนเอกสาร และนำเสนอแนวคิดอย่างมั่นใจ",
                "พัฒนาทักษะสื่อสารระหว่าง Developer, ผู้ใช้งาน และฝ่ายบริหารธุรกิจ",
                new[]
                {
                    ("หลักการสื่อสารทางเทคนิคให้เข้าใจง่ายสำหรับ Non-tech Stakeholders", "การแปลงศัพท์เทคนิคเป็นผลประโยชน์ทางธุรกิจ และการเลือกใช้ภาพเปรียบเทียบ", "https://www.youtube.com/embed/i5mYphUoOCs"),
                    ("การเขียนอีเมล, เอกสาร Technical Specs และ Release Notes ที่กระชับ", "โครงสร้างการเขียนที่ผู้อ่านสามารถเข้าใจประเด็นสำคัญได้ภายในเวลาอันสั้น", "https://www.youtube.com/embed/q0_x9X9X9wA"),
                    ("การจัดโครงสร้างสไลด์และการนำเสนอผลงานเดโมระบบอย่างมั่นใจ", "การเตรียมสไลด์ที่ไม่รกด้วยข้อความ และการเล่าเรื่องเพื่อสร้างความประทับใจ", "https://www.youtube.com/embed/V8eLdbKXGzk"),
                    ("ศิลปะการให้และรับ Feedback ในการ Code Review และการทำงานร่วมกัน", "การสื่อสารเชิงสร้างสรรค์ในการตรวจสอบโค้ด และการแยกเรื่องงานออกจากเรื่องส่วนตัว", "https://www.youtube.com/embed/w7W8V1t1oOo")
                }
            ),

            // 24. Time Management & Deep Work
            new(
                "time-management-deep-work",
                "การจัดการเวลาและโฟกัสสำหรับการเรียนรู้",
                "Productivity",
                "https://images.unsplash.com/photo-1506784983877-45594efa4cbe?auto=format&fit=crop&w=800&q=80",
                "วางแผนงาน แบ่งเป้าหมาย และสร้างกิจวัตรการเรียนที่ทำต่อเนื่องได้",
                "เครื่องมือจัดการเวลาและพลังงานสำหรับการทำงานและการเรียนรู้ตลอดชีวิต",
                new[]
                {
                    ("แนวคิด Deep Work และการปกป้องเวลาสำหรับการคิดเชิงลึก", "ความแตกต่างระหว่างงานที่ใช้สมาธิสูง (Deep Work) และงานประจำวันทั่วไป (Shallow Work)", "https://www.youtube.com/embed/mBwI7dDqS4Q"),
                    ("การจัดลำดับความสำคัญด้วย Eisenhower Matrix และ Time Blocking", "การแบ่งเวลาลงปฏิทินล่วงหน้าเพื่อโฟกัสงานสำคัญโดยไม่ถูกรบกวน", "https://www.youtube.com/embed/y2X7c9TUQJ8"),
                    ("การจัดการกับสิ่งรบกวน (Distractions) และการสร้างระบบโฟกัส", "เทคนิคการตัดการแจ้งเตือนและการสร้างสภาพแวดล้อมที่เอื้อต่อการเรียนรู้ต่อเนื่อง", "https://www.youtube.com/embed/0b_Xb_x_0kM")
                }
            ),

            // 25. Personal Finance
            new(
                "personal-finance-budgeting",
                "พื้นฐานการเงินส่วนบุคคลและการวางแผนงบ",
                "Personal Development",
                "https://images.unsplash.com/photo-1579621970563-ebec7560ff3e?auto=format&fit=crop&w=800&q=80",
                "จัดทำงบประมาณ เข้าใจเงินสำรอง หนี้ และเป้าหมายทางการเงินส่วนบุคคล",
                "เริ่มจัดการเงินด้วยหลักการพื้นฐานและตัวอย่างแผนรายเดือนที่ทำตามได้จริง",
                new[]
                {
                    ("การจัดทำงบประมาณรายรับ-รายจ่ายด้วยกฎ 50/30/20", "การแบ่งสัดส่วนเงินสำหรับสิ่งที่จำเป็น (Needs), สิ่งที่ต้องการ (Wants) และเงินออม (Savings)", "https://www.youtube.com/embed/HQzoZfc3GwQ"),
                    ("การสร้างเงินสำรองฉุกเฉินและการจัดการหนี้สินอย่างมีกลยุทธ์", "คำนวณเงินสำรอง 3-6 เดือน และเทคนิคการปลดหนี้ด้วยวิธี Snowball หรือ Avalanche", "https://www.youtube.com/embed/3b_0_k9_w8U"),
                    ("การตั้งเป้าหมายทางการเงินและการวางแผนออมเงินระยะสั้น-ระยะยาว", "การใช้หลัก SMART กำหนดเป้าหมายทางการเงิน และการวางแผนภาษีเบื้องต้น", "https://www.youtube.com/embed/rK8v0_w_XwU"),
                    ("พื้นฐานการลงทุนสำหรับคนรุ่นใหม่: กองทุนรวมและสินทรัพย์ต่างๆ", "ทำความเข้าใจความเสี่ยง ผลตอบแทน และพลังของดอกเบี้ยทบต้นในการสะสมความมั่งคั่ง", "https://www.youtube.com/embed/WEDIj9JBTC8")
                }
            ),

            // 26. Digital Marketing
            new(
                "digital-marketing-small-business",
                "พื้นฐานการตลาดดิจิทัลสำหรับธุรกิจเล็ก",
                "Digital Marketing",
                "https://images.unsplash.com/photo-1432888498266-38ffec3eaf0a?auto=format&fit=crop&w=800&q=80",
                "รู้จักกลุ่มเป้าหมาย Content, SEO และการวัดผลแคมเปญเบื้องต้น",
                "วางแผนการตลาดดิจิทัลแบบเริ่มต้นสำหรับร้านค้าและธุรกิจขนาดเล็กได้อย่างมีทิศทาง",
                new[]
                {
                    ("การวิเคราะห์กลุ่มเป้าหมาย (Target Audience) และ Customer Journey", "การค้นหาลูกค้าตัวจริง การสร้าง Customer Persona และการวิเคราะห์ขั้นตอนการตัดสินใจซื้อ", "https://www.youtube.com/embed/nU-IIXBWlS4"),
                    ("การวางแผน Content Marketing ที่ดึงดูดและสร้างการมีส่วนร่วม", "การสร้างเนื้อหาที่ตอบโจทย์ Pain Point ของลูกค้า และการสร้างความน่าเชื่อถือให้แบรนด์", "https://www.youtube.com/embed/Wb_0_k9_22A"),
                    ("พื้นฐานการทำ Search Engine Optimization (SEO) เบื้องต้น", "การค้นหาคำค้นหา (Keyword Research) และการปรับแต่งโครงสร้างเนื้อหาบนหน้าเว็บ", "https://www.youtube.com/embed/xsVTqzratPs"),
                    ("การวัดผลและวิเคราะห์ตัวชี้วัดสำคัญ (CTR, Conversion Rate, ROI)", "การอ่านค่าสถิติจากแดชบอร์ดการตลาดเพื่อปรับปรุงและเพิ่มประสิทธิภาพงบประมาณ", "https://www.youtube.com/embed/1_0_w8r_xXk")
                }
            ),

            // 27. Content Writing
            new(
                "content-writing-clear-copy",
                "เขียนคอนเทนต์ให้อ่านง่ายและน่าสนใจ",
                "Digital Marketing",
                "https://images.unsplash.com/photo-1455390582262-044cdead277a?auto=format&fit=crop&w=800&q=80",
                "ฝึกวางโครงเรื่อง เขียนหัวข้อ และปรับข้อความให้เหมาะกับผู้อ่าน",
                "เรียนรู้หลักการเขียนคอนเทนต์ที่ชัดเจน มีประโยชน์ และสื่อสารตรงเป้าหมาย",
                new[]
                {
                    ("พลังของพาดหัว (Headlines): เขียนอย่างไรให้คนหยุดอ่านภายใน 3 วินาที", "เทคนิคการตั้งชื่อบทความให้น่าสนใจ การกระตุ้นความอยากรู้ และการระบุคุณประโยชน์ที่ชัดเจน", "https://www.youtube.com/embed/mKk_Xq3P3eU"),
                    ("โครงสร้างการเล่าเรื่อง (Storytelling) และสูตรการเขียนแบบ AIDA", "Attention, Interest, Desire, Action: ลำดับการเล่าเรื่องเพื่อโน้มน้าวใจผู้อ่านอย่างเป็นธรรมชาติ", "https://www.youtube.com/embed/2b_0_k8_1wA"),
                    ("การปรับโทนเสียง (Brand Voice) ให้สอดคล้องกับภาพลักษณ์ของแบรนด์", "การเลือกใช้ระดับภาษา ความเป็นกันเอง และความน่าเชื่อถือที่เหมาะกับกลุ่มเป้าหมาย", "https://www.youtube.com/embed/3b_0_w8_2xA"),
                    ("การตรวจทานและขัดเกลาบทความให้กระชับ อ่านง่าย และทรงพลัง", "การตัดคำฟุ่มเฟือย การเว้นวรรคให้อ่านสบายตาบนหน้าจอมือถือ และการวาง Call to Action", "https://www.youtube.com/embed/4b_0_w9_3yA")
                }
            ),

            // 28. Machine Learning
            new(
                "machine-learning-python-basics",
                "พื้นฐาน Machine Learning ด้วย Python",
                "AI & Machine Learning",
                "https://images.unsplash.com/photo-1555255707-c07966088b7b?auto=format&fit=crop&w=800&q=80",
                "รู้จักชุดข้อมูล การแบ่ง Train/Test และแนวคิดโมเดลจำแนกประเภท",
                "ปูพื้นฐาน Machine Learning ด้วยตัวอย่างเชิงการศึกษาและโมเดลที่ใช้งานได้จริง",
                new[]
                {
                    ("ภาพรวมของ Machine Learning: Supervised, Unsupervised และ Reinforcement", "ทำความเข้าใจชนิดของการเรียนรู้ของเครื่อง ตัวอย่างการประยุกต์ใช้ในอุตสาหกรรม", "https://www.youtube.com/embed/aircAruvnKk"),
                    ("การเตรียมข้อมูลและการทำ Feature Scaling ด้วย Scikit-Learn", "การจัดการค่าว่าง การแปลงข้อมูล Categorical และการปรับมาตรวัดข้อมูล (Scaling)", "https://www.youtube.com/embed/IHZwWFHWa-w"),
                    ("การสร้างและประเมินโมเดล Classification (Logistic Regression)", "การฝึกสอนโมเดล (model.fit) การทำนายผล (model.predict) และการวัดความแม่นยำ (Accuracy, F1-Score)", "https://www.youtube.com/embed/Ilg3gGewQ5U"),
                    ("ปัญหา Overfitting และ Underfitting พร้อมวิธีแก้ไขด้วย Cross-Validation", "การแบ่งชุดข้อมูล Train/Validation/Test อย่างมีมาตรฐานเพื่อป้องกันโมเดลจำข้อสอบ", "https://www.youtube.com/embed/EuBBz3bI-aA"),
                    ("เวิร์กช็อปทำนายผลข้อมูลด้วย Scikit-Learn Pipeline", "การนำกระบวนการเตรียมข้อมูลและโมเดลมารวมเป็น Pipeline เดียวเพื่อการ Deploy อย่างมีระบบ", "https://www.youtube.com/embed/0Lt9w-BxKFQ")
                }
            ),

            // 29. Prompt Engineering
            new(
                "prompt-engineering-work",
                "Prompt Engineering สำหรับการทำงาน",
                "AI & Machine Learning",
                "https://images.unsplash.com/photo-1620712943543-bcc4688e7485?auto=format&fit=crop&w=800&q=80",
                "ออกแบบคำสั่งให้ชัดเจน ระบุบริบท และตรวจสอบผลลัพธ์ AI อย่างรับผิดชอบ",
                "ฝึกใช้ Generative AI เป็นผู้ช่วยงานเขียน สรุป และระดมความคิดอย่างมีประสิทธิภาพสูงสุด",
                new[]
                {
                    ("โครงสร้าง Prompt ที่มีประสิทธิภาพ: Role, Context, Task, Constraints, Format", "หลักการเขียนคำสั่งที่ชัดเจน ไม่คลุมเครือ เพื่อให้ AI คืนคำตอบที่ตรงตามความต้องการที่สุด", "https://www.youtube.com/embed/R8_veQiYBjI"),
                    ("เทคนิคขั้นสูง: Few-Shot Prompting และ Chain-of-Thought (CoT)", "การให้ตัวอย่างคำตอบประกอบ และการกระตุ้นให้ AI อธิบายขั้นตอนการคิดทีละขั้นเพื่อลดความผิดพลาด", "https://www.youtube.com/embed/dOxUroR57xs"),
                    ("การประยุกต์ใช้ AI ในงานจริง: สรุปเอกสาร, เขียนโค้ด, และวิเคราะห์ข้อมูล", "เวิร์กช็อปการเขียน Prompt สำหรับงานสรุปรายงานยาวๆ และการให้ AI ช่วยตรวจจับบั๊กในโค้ด", "https://www.youtube.com/embed/9-Jl0dxWQs8"),
                    ("จริยธรรม ความปลอดภัยของข้อมูล และการตรวจสอบความถูกต้อง (Hallucination)", "ข้อควรระวังเรื่องข้อมูลลับขององค์กร และเทคนิคการตรวจสอบข้อเท็จจริงเพื่อป้องกัน AI แต่งข้อมูล", "https://www.youtube.com/embed/bPVaOlJ6ln0")
                }
            )
        };
    }
}
