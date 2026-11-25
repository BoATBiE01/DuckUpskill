using DuckUpskill.Models;
using Microsoft.EntityFrameworkCore;

namespace DuckUpskill.Data;

public static class CourseAssessmentSeeder
{
    public record CourseQuizData(
        string CourseSlug,
        (string Question, string[] Options, int CorrectIndex)[] PreTests,
        (string Question, string[] Options, int CorrectIndex)[] PostTests
    );

    public static async Task SeedOrUpdateAssessmentsAsync(ApplicationDbContext context)
    {
        var quizzes = GetCuratedQuizzes();

        foreach (var item in quizzes)
        {
            var course = await context.Courses.FirstOrDefaultAsync(c => c.Slug == item.CourseSlug);
            if (course == null) continue;

            // Update or create Pre-test questions
            await SeedQuestionsForTypeAsync(context, course.Id, AssessmentType.PreTest, item.PreTests);

            // Update or create Post-test questions
            await SeedQuestionsForTypeAsync(context, course.Id, AssessmentType.PostTest, item.PostTests);
        }

        await context.SaveChangesAsync();
        Console.WriteLine("[DuckUpskill] อัปเดตข้อสอบ Pre-test และ Post-test คุณภาพสูงตรงหลักสูตรครบ 28 คอร์สเรียบร้อย");
    }

    private static async Task SeedQuestionsForTypeAsync(
        ApplicationDbContext context,
        int courseId,
        AssessmentType type,
        (string Question, string[] Options, int CorrectIndex)[] questionsData)
    {
        var existingQuestions = await context.AssessmentQuestions
            .Where(q => q.CourseId == courseId && q.Type == type)
            .Include(q => q.Options)
            .OrderBy(q => q.OrderNumber)
            .ToListAsync();

        for (int i = 0; i < questionsData.Length; i++)
        {
            var data = questionsData[i];
            var orderNum = i + 1;

            if (i < existingQuestions.Count)
            {
                var existing = existingQuestions[i];
                existing.Text = data.Question;
                existing.OrderNumber = orderNum;

                var existingOptions = existing.Options.OrderBy(o => o.OrderNumber).ToList();
                for (int optIdx = 0; optIdx < 4 && optIdx < data.Options.Length; optIdx++)
                {
                    if (optIdx < existingOptions.Count)
                    {
                        existingOptions[optIdx].Text = data.Options[optIdx];
                        existingOptions[optIdx].IsCorrect = (optIdx == data.CorrectIndex);
                        existingOptions[optIdx].OrderNumber = optIdx + 1;
                    }
                    else
                    {
                        existing.Options.Add(new AssessmentOption
                        {
                            AssessmentQuestionId = existing.Id,
                            Text = data.Options[optIdx],
                            IsCorrect = (optIdx == data.CorrectIndex),
                            OrderNumber = optIdx + 1
                        });
                    }
                }
            }
            else
            {
                var newQuestion = new AssessmentQuestion
                {
                    CourseId = courseId,
                    Type = type,
                    OrderNumber = orderNum,
                    Text = data.Question,
                    Options = data.Options.Select((optText, optIdx) => new AssessmentOption
                    {
                        Text = optText,
                        IsCorrect = (optIdx == data.CorrectIndex),
                        OrderNumber = optIdx + 1
                    }).ToList()
                };
                context.AssessmentQuestions.Add(newQuestion);
            }
        }
    }

    public static List<CourseQuizData> GetCuratedQuizzes()
    {
        return new List<CourseQuizData>
        {
            // 1. intro-html-css-javascript
            new(
                "intro-html-css-javascript",
                new[]
                {
                    ("แท็กใดใน HTML5 ใช้สำหรับกำหนดส่วนหัวเรื่องหลักที่มีความสำคัญสูงสุด?", new[] { "<p>", "<h1>", "<header>", "<title>" }, 1),
                    ("คุณสมบัติ CSS ใดใช้สำหรับจัดระยะห่างภายในระหว่างเนื้อหากับเส้นขอบ?", new[] { "margin", "padding", "border", "gap" }, 1),
                    ("คำสั่งใดใน JavaScript ใช้สำหรับประกาศตัวแปรที่สามารถเปลี่ยนแปลงค่าได้ในภายหลัง?", new[] { "const", "static", "let", "readonly" }, 2)
                },
                new[]
                {
                    ("Semantic HTML มีประโยชน์หลักอย่างไรต่อการพัฒนาเว็บไซต์สมัยใหม่?", new[] { "ทำให้ดาวน์โหลดหน้าเว็บได้เร็วขึ้นเป็น 2 เท่า", "ช่วยเรื่อง SEO และ Screen Reader อ่านโครงสร้างหน้าเว็บได้อย่างถูกต้อง", "ทำให้ไม่ต้องเขียนโค้ด CSS สำหรับตกแต่ง", "ช่วยให้รันบนเซิร์ฟเวอร์ได้โดยตรง" }, 1),
                    ("ใน CSS Flexbox หากต้องการให้ลูกๆ กระจายตัวในแนวแกนหลัก (Main Axis) ต้องใช้คำสั่งใด?", new[] { "align-items", "justify-content", "flex-direction", "align-content" }, 1),
                    ("คำสั่ง addEventListener('click', handler) ใน JavaScript มีหน้าที่อะไร?", new[] { "สร้างปุ่มคลิกใหม่บนหน้าจอ", "ดักจับและตอบสนองเมื่อผู้ใช้กดคลิกที่ Element นั้น", "ลบ Element ออกจากหน้าจอ", "รีเฟรชหน้าเว็บอัตโนมัติ" }, 1),
                    ("แท็ก <a> ใน HTML ต้องระบุ Attribute ใดเพื่อระบุ URL ปลายทาง?", new[] { "src", "link", "href", "target" }, 2)
                }
            ),

            // 2. fullstack-aspnet-core-mvc
            new(
                "fullstack-aspnet-core-mvc",
                new[]
                {
                    ("ในสถาปัตยกรรม MVC ตัวอักษร 'C' หมายถึงสิ่งใดและทำหน้าที่อะไร?", new[] { "Component - แสดงผลหน้าจอ", "Controller - รับ Request และประสานงานระหว่าง Model กับ View", "Configuration - ตั้งค่าระบบ", "Compiler - แปลงภาษาโปรแกรม" }, 1),
                    ("Entity Framework Core จัดเป็นเครื่องมือประเภทใด?", new[] { "Web Server", "Database Engine", "Object-Relational Mapping (ORM)", "Frontend Framework" }, 2),
                    ("Attribute ใดใช้สำหรับจำกัดการเข้าถึง Action ใน Controller ให้เฉพาะผู้ที่ล็อกอินแล้ว?", new[] { "[AllowAnonymous]", "[Authorize]", "[HttpGet]", "[ValidateAntiForgeryToken]" }, 1)
                },
                new[]
                {
                    ("Dependency Injection (DI) ใน ASP.NET Core ช่วยแก้ปัญหาใดเป็นหลัก?", new[] { "ช่วยลดความผูกมัดแน่น (Tightly-coupled) ระหว่างคลาสและทำให้ทดสอบง่ายขึ้น", "ช่วยให้รันโค้ดภาษา Python ได้ใน C#", "ช่วยบีบอัดไฟล์รูปภาพให้อัตโนมัติ", "แทนที่การใช้งานฐานข้อมูล SQL Server" }, 0),
                    ("คำสั่งใดใน Entity Framework Core ใช้สำหรับบันทึกการเปลี่ยนแปลงทั้งหมดลงสู่ฐานข้อมูล?", new[] { "context.Commit()", "context.SaveChangesAsync()", "context.Database.Push()", "context.ApplyChanges()" }, 1),
                    ("Attribute [ValidateAntiForgeryToken] มีบทบาทสำคัญในการป้องกันภัยคุกคามประเภทใด?", new[] { "SQL Injection", "Cross-Site Scripting (XSS)", "Cross-Site Request Forgery (CSRF)", "Distributed Denial of Service (DDoS)" }, 2),
                    ("การทำ Role-based Authorization เช่น [Authorize(Roles = \"Admin\")] มีผลอย่างไร?", new[] { "ผู้ใช้ทุกคนสามารถเข้าใช้งานได้", "เฉพาะผู้ใช้ที่มีบทบาท Admin เท่านั้นที่เข้าถึง Action นี้ได้", "ระบบจะสร้างผู้ใช้ Admin ใหม่อัตโนมัติ", "จะรีเซ็ตรหัสผ่านของผู้ใช้ทุกคนในระบบ" }, 1)
                }
            ),

            // 3. git-github-fundamentals
            new(
                "git-github-fundamentals",
                new[]
                {
                    ("สถานะใดใน Git หมายถึงไฟล์ถูกเตรียมพร้อมเพื่อบันทึกลง Commit ถัดไป?", new[] { "Working Directory", "Staging Area (Index)", "Remote Repository", "Untracked" }, 1),
                    ("คำสั่งใดใช้สำหรับสร้างและสลับไปยัง Branch ใหม่พร้อมกัน?", new[] { "git branch new-feature", "git switch -c new-feature", "git commit -b new-feature", "git merge new-feature" }, 1),
                    ("คำสั่งใดใช้สำหรับดาวน์โหลดโค้ดจาก Remote Repository มายังเครื่องตนเองเป็นครั้งแรก?", new[] { "git pull", "git fetch", "git clone", "git init" }, 2)
                },
                new[]
                {
                    ("เมื่อเกิด Merge Conflict ขั้นตอนที่ถูกต้องในการจัดการคือข้อใด?", new[] { "ลบโฟลเดอร์ .git ทิ้งแล้วดาวน์โหลดใหม่", "เปิดไฟล์แก้ไขส่วนที่ขัดแย้ง บันทึกไฟล์ สั่ง git add และทำการ commit", "สั่ง git push ทันทีเพื่อทับโค้ดเดิม", "รีสตาร์ตเครื่องคอมพิวเตอร์" }, 1),
                    ("คำสั่ง git pull มีการทำงานเทียบเท่ากับการรวมคำสั่งใดสองคำสั่งเข้าด้วยกัน?", new[] { "git add + git commit", "git fetch + git merge", "git status + git log", "git push + git branch" }, 1),
                    ("Pull Request (PR) บน GitHub มีจุดประสงค์หลักเพื่ออะไร?", new[] { "บังคับให้โค้ดถูกรวมเข้าสู่ production ทันที", "ขอให้ทีมงานตรวจสอบและรีวิวโค้ด (Code Review) ก่อนรวมเข้า Branch หลัก", "ใช้สำหรับแจ้งลบบัญชีผู้ใช้", "ใช้สำหรับสำรองไฟล์เอกสาร Word" }, 1)
                }
            ),

            // 4. building-ai-apps-dotnet-llm
            new(
                "building-ai-apps-dotnet-llm",
                new[]
                {
                    ("ในการเรียกใช้งาน LLM คำว่า 'Token' หมายถึงสิ่งใด?", new[] { "รหัสผ่านสำหรับการเชื่อมต่อ API", "หน่วยย่อยของคำหรือตัวอักษรที่โมเดลใช้ในการประมวลผล", "ประเภทของฐานข้อมูล", "ใบรับรองความปลอดภัย SSL" }, 1),
                    ("การปรับค่า 'Temperature' ในการเรียก AI ส่งผลต่อผลลัพธ์อย่างไร?", new[] { "ปรับความเร็วของการเชื่อมต่ออินเทอร์เน็ต", "ควบคุมความสุ่มและความคิดสร้างสรรค์ของคำตอบ", "ควบคุมจำนวนตัวอักษรสูงสุด", "ปรับความสว่างของหน้าจอ" }, 1),
                    ("เทคนิค RAG (Retrieval-Augmented Generation) มีประโยชน์หลักอย่างไร?", new[] { "สอนโมเดล AI ใหม่ตั้งแต่ศูนย์", "ดึงข้อมูลภายนอกที่เกี่ยวข้องส่งให้ AI เพื่อใช้ตอบคำถามได้อย่างแม่นยำขึ้น", "แปลงไฟล์ C# ให้เป็นภาษา Assembly", "เพิ่มแรมให้กับเครื่องคอมพิวเตอร์" }, 1)
                },
                new[]
                {
                    ("Streaming Response ในการเรียก AI มีประโยชน์อย่างไรต่อ User Experience (UX)?", new[] { "ผู้ใช้ไม่ต้องรอนาน โดยจะเห็นข้อความค่อยๆ ทยอยแสดงออกมาทันทีที่สร้างเสร็จ", "ทำให้ลดการใช้ Tokens ลง 50%", "ทำให้ AI ตอบคำถามได้ถูกต้อง 100%", "ช่วยให้ไม่ต้องใช้อินเทอร์เน็ตในการเรียก" }, 0),
                    ("Function Calling / Tool Use อนุญาตให้ AI ทำสิ่งใด?", new[] { "เขียนโค้ดคอมไพล์โปรแกรมเองโดยไม่ต้องมีคนสั่ง", "ตัดสินใจส่งชื่อฟังก์ชันและพารามิเตอร์กลับมาให้โปรแกรมของเราเรียกใช้งานระบบจริง", "เข้าถึงข้อมูลลับในเครื่องของผู้ใช้โดยไม่ขออนุญาต", "เปลี่ยนโมเดลภาษาให้เป็นโมเดลภาพ" }, 1),
                    ("ข้อใดคือแนวปฏิบัติที่ดีในการเก็บรักษา API Key ของ AI ในโปรเจกต์ .NET?", new[] { "เขียนใส่ Hardcode ในไฟล์ Controller โดยตรง", "บันทึกใน User Secrets หรือ Environment Variables และไม่ Push ขึ้น GitHub", "ส่งไปใน URL Query String", "เก็บไว้ในโฟลเดอร์ wwwroot" }, 1)
                }
            ),

            // 5. sql-server-database-design
            new(
                "sql-server-database-design",
                new[]
                {
                    ("Primary Key ในตารางฐานข้อมูลเชิงสัมพันธ์มีคุณสมบัติสำคัญอย่างไร?", new[] { "ต้องมีค่าซ้ำกันได้เสมอ", "ค่าต้องไม่ซ้ำกัน (Unique) และห้ามเป็นค่าว่าง (Not Null)", "เก็บได้เฉพาะตัวอักษรเท่านั้น", "มีได้หลายคอลัมน์ในตารางเดียวโดยอิสระ" }, 1),
                    ("Foreign Key มีบทบาทสำคัญในการทำสิ่งใด?", new[] { "เร่งความเร็วในการเปิดโปรแกรม", "สร้างความสัมพันธ์และบังคับความถูกต้องของข้อมูลระหว่างสองตาราง (Referential Integrity)", "ใช้สำหรับเข้ารหัสข้อมูลในฮาร์ดดิสก์", "ใช้สำรองข้อมูลไปยังเครื่องอื่น" }, 1)
                },
                new[]
                {
                    ("การทำ Database Normalization (1NF, 2NF, 3NF) มีเป้าหมายสูงสุดเพื่ออะไร?", new[] { "ลดความซ้ำซ้อนของข้อมูลและป้องกันความผิดปกติในการแก้ไข (Anomalies)", "ทำให้ขนาดไฟล์ฐานข้อมูลมีขนาดใหญ่ขึ้น", "ทำให้ไม่ต้องเขียนคำสั่ง SQL ในการค้นหา", "ช่วยให้ตารางไม่มี Foreign Key" }, 0),
                    ("คำสั่ง SQL ชนิดใดใช้สำหรับรวมข้อมูลจากสองตารางโดยคืนค่าเฉพาะแถวที่มีค่าตรงกันทั้งสองฝั่ง?", new[] { "LEFT JOIN", "FULL OUTER JOIN", "INNER JOIN", "CROSS JOIN" }, 2),
                    ("การสร้าง Non-Clustered Index บนคอลัมน์ที่ถูกค้นหาบ่อยๆ ส่งผลดีอย่างไร?", new[] { "ลดขนาดของฐานข้อมูล", "ช่วยให้ค้นหาและดึงข้อมูลได้รวดเร็วขึ้นอย่างมาก", "ทำให้ไม่ต้องใช้คำสั่ง WHERE", "ทำให้สามารถเก็บค่า Null ได้ไม่จำกัด" }, 1)
                }
            ),

            // 6. tech-soft-skills-interview
            new(
                "tech-soft-skills-interview",
                new[]
                {
                    ("ในการตอบคำถามสัมภาษณ์เชิงพฤติกรรม (Behavioral Interview) ตัวย่อ STAR ย่อมาจากอะไร?", new[] { "Skill, Team, Action, Review", "Situation, Task, Action, Result", "Strategy, Target, Ability, Reward", "Speed, Time, Accuracy, Report" }, 1),
                    ("เรซูเม่ที่ดีสำหรับสาย Tech ควรมีลักษณะตามข้อใดมากที่สุด?", new[] { "เขียนยาว 10 หน้าเพื่อแสดงความละเอียด", "กระชับ ตรงประเด็น เน้นผลลัพธ์ที่วัดผลได้ (Impact) และทักษะที่เกี่ยวข้อง", "ใช้สีสันฉูดฉาดเพื่อดึงดูดสายตา", "ไม่ต้องระบุช่องทางการติดต่อเพื่อความเป็นส่วนตัว" }, 1)
                },
                new[]
                {
                    ("เมื่อถูกถามถึง 'ความผิดพลาดครั้งใหญ่ที่สุดในการทำงาน' แนวทางตอบที่ได้คะแนนดีที่สุดคือข้อใด?", new[] { "บอกว่าตนเองไม่เคยทำผิดพลาดเลยแม้แต่ครั้งเดียว", "เล่าสถานการณ์ที่เกิดขึ้น ความรับผิดชอบ สิ่งที่ได้เรียนรู้ และการปรับปรุงเพื่อไม่ให้เกิดซ้ำ", "โยนความผิดให้เพื่อนร่วมทีมหรือหัวหน้างาน", "เปลี่ยนเรื่องคุยทันที" }, 1),
                    ("ในทีมพัฒนาซอฟต์แวร์ การให้ Feedback ในการ Code Review ที่สร้างสรรค์ควรเป็นอย่างไร?", new[] { "วิจารณ์ตัวบุคคลเพื่อความจริงใจ", "มุ่งเน้นที่คุณภาพของโค้ด อธิบายเหตุผล พร้อมเสนอแนะทางเลือกในการปรับปรุง", "กด Approve เสมอโดยไม่ต้องอ่าน", "สั่งให้แก้ตามใจตนเองโดยไม่มีเหตุผลทางเทคนิค" }, 1)
                }
            ),

            // 8. python-beginner-projects
            new(
                "python-beginner-projects",
                new[]
                {
                    ("ฟังก์ชันใดใน Python ใช้สำหรับรับข้อมูลอินพุตจากผู้ใช้งานผ่านแป้นพิมพ์?", new[] { "get()", "read()", "input()", "scan()" }, 2),
                    ("ชนิดข้อมูล [1, 2, 3] ในภาษา Python จัดเป็นชนิดข้อมูลแบบใด?", new[] { "Tuple", "Dictionary", "List", "Set" }, 2),
                    ("ผลลัพธ์ของคำสั่ง type(3.14) ใน Python คือข้อใด?", new[] { "<class 'int'>", "<class 'float'>", "<class 'double'>", "<class 'decimal'>" }, 1)
                },
                new[]
                {
                    ("ข้อใดคือความแตกต่างสำคัญระหว่าง List และ Tuple ในภาษา Python?", new[] { "List แก้ไขค่าได้ (Mutable) แต่ Tuple แก้ไขค่าไม่ได้ (Immutable)", "Tuple แก้ไขค่าได้ แต่ List แก้ไขไม่ได้", "List เก็บได้เฉพาะตัวเลขเท่านั้น", "ไม่มีความแตกต่างกันเลย" }, 0),
                    ("บล็อกคำสั่งใดใช้สำหรับจัดการข้อผิดพลาดเพื่อไม่ให้โปรแกรม Python หยุดทำงานกะทันหัน?", new[] { "try - except", "catch - throw", "if - error", "begin - rescue" }, 0),
                    ("คำสั่งใดใช้สำหรับออกจากลูปการทำงานก่อนที่เงื่อนไขจะสิ้นสุดทันที?", new[] { "continue", "pass", "break", "return" }, 2),
                    ("ฟังก์ชันใน Python ประกาศเริ่มต้นด้วยคีย์เวิร์ดใด?", new[] { "function", "func", "def", "method" }, 2)
                }
            ),

            // 9. csharp-dotnet-foundations
            new(
                "csharp-dotnet-foundations",
                new[]
                {
                    ("คีย์เวิร์ดใดใน C# ใช้สำหรับสร้าง Instance ใหม่ของคลาสในหน่วยความจำ?", new[] { "create", "new", "make", "instantiate" }, 1),
                    ("ชนิดข้อมูลใดใน C# เหมาะที่สุดสำหรับการเก็บค่าเงินที่มีความแม่นยำสูง?", new[] { "float", "double", "decimal", "int" }, 2),
                    ("คลาสใน C# จัดเป็นชนิดข้อมูลประเภทใด?", new[] { "Value Type", "Reference Type", "Primitive Type", "Pointer Type" }, 1)
                },
                new[]
                {
                    ("หลักการ Encapsulation ใน OOP มีประโยชน์หลักในเรื่องใด?", new[] { "การสืบทอดคุณสมบัติจากคลาสแม่", "การปกป้องข้อมูลภายในออบเจ็กต์และเปิดให้เข้าถึงผ่าน Properties ที่ควบคุมได้", "การแปลงโค้ดให้รันเร็วขึ้น 10 เท่า", "การรันโปรแกรมข้ามแพลตฟอร์ม" }, 1),
                    ("คำสั่ง LINQ ข้อใดใช้สำหรับกรองข้อมูลใน List ตามเงื่อนไขที่กำหนด?", new[] { "items.Select(x => x > 5)", "items.Where(x => x > 5)", "items.Filter(x => x > 5)", "items.FindAll(x => x > 5)" }, 1),
                    ("Interface ใน C# มีลักษณะสำคัญอย่างไร?", new[] { "สามารถมีตัวแปร Instance ได้ทุกชนิด", "กำหนดข้อตกลง (Contract) ของเมธอดโดยไม่มีโค้ดการทำงาน เพื่อให้คลาสนำไป Implement", "สามารถสร้าง Instance ด้วยคำสั่ง new ได้โดยตรง", "ห้ามมีเมธอดเกิน 5 ตัว" }, 1),
                    ("คำสั่ง dotnet CLI ข้อใดใช้สำหรับคอมไพล์และเปิดรันโปรเจกต์?", new[] { "dotnet restore", "dotnet new", "dotnet run", "dotnet pack" }, 2)
                }
            ),

            // 10. react-component-web
            new(
                "react-component-web",
                new[]
                {
                    ("JSX ใน React ย่อมาจากอะไรและมีลักษณะอย่างไร?", new[] { "Java Syntax Extension", "JavaScript XML - ไวยากรณ์ที่ทำให้เขียนโค้ดคล้าย HTML ลงในไฟล์ JavaScript ได้", "JSON System Extension", "Just Style Sheets" }, 1),
                    ("Hook ใดใน React ใช้สำหรับจัดการ State ภายใน Function Component?", new[] { "useEffect", "useContext", "useState", "useReducer" }, 2),
                    ("Props ใน React มีลักษณะการถ่ายทอดข้อมูลแบบใด?", new[] { "จากลูกขึ้นไปหาแม่", "สองทิศทางแบบอัตโนมัติ", "ทางเดียวจากแม่ลงสู่ลูก (One-way Data Binding)", "กระจายไปทุก Component ในระบบเสมอ" }, 2)
                },
                new[]
                {
                    ("ทำไมการ Render รายการใน React ด้วยฟังก์ชัน .map() ต้องระบุ attribute 'key' ให้กับ Element?", new[] { "เพื่อความสวยงามใน CSS", "เพื่อช่วยให้ React Virtual DOM ระบุและอัปเดตเฉพาะ Element ที่เปลี่ยนแปลงได้อย่างมีประสิทธิภาพ", "เพื่อให้ปุ่มกดคลิกได้", "เป็นข้อกำหนดของภาษา HTML5" }, 1),
                    ("useEffect ที่ใส่ Dependency Array ว่างเปล่า [] จะทำงานเมื่อใด?", new[] { "ทำงานทุกครั้งที่มีการ Re-render", "ทำงานเพียงครั้งเดียวเมื่อ Component ถูก Mount ขึ้นบนหน้าจอ", "ไม่ทำงานเลย", "ทำงานเฉพาะเมื่อกดปุ่ม Submit" }, 1),
                    ("Virtual DOM ใน React มีบทบาทสำคัญในการทำสิ่งใด?", new[] { "เก็บข้อมูลใน LocalStorage ของเบราว์เซอร์", "เปรียบเทียบการเปลี่ยนแปลง (Diffing) ก่อนอัปเดต Real DOM จริงเพื่อเพิ่มประสิทธิภาพ", "เชื่อมต่อฐานข้อมูล MongoDB", "แปลงโค้ด React เป็นแอปพลิเคชันมือถือ" }, 1),
                    ("ข้อใดคือวิธีที่ถูกต้องในการอัปเดตค่า State ใน React?", new[] { "count = count + 1", "setCount(count + 1)", "this.count++", "updateState('count', 1)" }, 1)
                }
            ),

            // 11. typescript-for-javascript
            new(
                "typescript-for-javascript",
                new[]
                {
                    ("ข้อดีหลักของ TypeScript เมื่อเทียบกับ JavaScript ธรรมดาคืออะไร?", new[] { "รันบนเบราว์เซอร์ได้โดยไม่ต้องใช้คอมไพเลอร์", "มีระบบ Static Typing ที่ช่วยจับข้อผิดพลาดตั้งแต่ตอนเขียนโค้ด (Compile-time)", "ทำให้ขนาดไฟล์โค้ดสั้นลง 90%", "ไม่จำเป็นต้องเขียนฟังก์ชัน" }, 1),
                    ("คำสั่งใดใช้สำหรับคอมไพล์ไฟล์ TypeScript ให้กลายเป็น JavaScript?", new[] { "npm compile", "tsc", "dotnet build", "ts-run" }, 1),
                    ("ชนิดข้อมูล 'any' ใน TypeScript ควรใช้งานเมื่อใด?", new[] { "ควรใช้กับทุกตัวแปรเพื่อความสะดวก", "ควรหลีกเลี่ยงเท่าที่เป็นไปได้ และใช้เฉพาะเมื่อจำเป็นจริงๆ เพื่อรักษาความปลอดภัยของ Type", "ใช้เมื่อต้องการให้โปรแกรมรันเร็วขึ้น", "ใช้เฉพาะกับตัวเลขเท่านั้น" }, 1)
                },
                new[]
                {
                    ("Union Type เช่น 'string | number' หมายถึงอะไร?", new[] { "ตัวแปรต้องเป็นทั้งสตริงและตัวเลขพร้อมกัน", "ตัวแปรสามารถเก็บค่าเป็นสตริงหรือตัวเลขชนิดใดชนิดหนึ่งก็ได้", "ตัวแปรเก็บเฉพาะค่าว่างเท่านั้น", "ตัวแปรจะถูกแปลงเป็น Boolean" }, 1),
                    ("Generics ใน TypeScript เช่นฟังก์ชัน <T>(arg: T): T มีประโยชน์หลักอย่างไร?", new[] { "ช่วยให้สร้างคอมโพเนนต์หรือฟังก์ชันที่ใช้ซ้ำได้กับหลายชนิดข้อมูลโดยไม่สูญเสีย Type Safety", "ช่วยให้รันบน Linux ได้", "ช่วยจัดรูปแบบโค้ดอัตโนมัติ", "ใช้สำหรับสร้าง User Interface" }, 0),
                    ("ข้อใดคือความแตกต่างหลักระหว่าง 'unknown' และ 'any' ใน TypeScript?", new[] { "unknown ไม่สามารถทำอะไรกับค่าได้เลยจนกว่าจะมีการตรวจสอบ Type (Type Guard)", "any มีความปลอดภัยกว่า unknown", "unknown ใช้ได้เฉพาะกับตัวเลข", "ทั้งสองตัวเหมือนกันทุกประการ" }, 0),
                    ("ไฟล์คอนฟิกหลักที่ใช้ควบคุมการทำงานของ TypeScript Compiler คือไฟล์ใด?", new[] { "package.json", "tsconfig.json", "settings.json", "webpack.config.js" }, 1)
                }
            ),

            // 12. ux-ui-design-fundamentals
            new(
                "ux-ui-design-fundamentals",
                new[]
                {
                    ("UX (User Experience) และ UI (User Interface) มีความแตกต่างกันอย่างไร?", new[] { "UX เน้นความรู้สึกและประสบการณ์ใช้งาน ส่วน UI เน้นความสวยงามและการจัดวางองค์ประกอบหน้าจอ", "UX สำหรับเว็บ ส่วน UI สำหรับแอปมือถือ", "UX คือการเขียนโค้ด ส่วน UI คือการวาดรูป", "ไม่มีความแตกต่างกัน" }, 0),
                    ("Persona ในกระบวนการออกแบบ UX มีประโยชน์เพื่อสิ่งใด?", new[] { "ใช้เป็นตัวแทนของกลุ่มเป้าหมายผู้ใช้งานจริง เพื่อช่วยให้ทีมตัดสินใจออกแบบได้ตรงจุด", "ใช้เป็นชื่อพนักงานในบริษัท", "ใช้สำหรับตั้งชื่อแบรนด์สินค้า", "ใช้สำหรับทดสอบเซิร์ฟเวอร์" }, 0),
                    ("Wireframe คืออะไรในการออกแบบหน้าจอ?", new[] { "หน้าจอที่มีการลงสีและรูปภาพสมบูรณ์แบบ", "ภาพร่างโครงสร้างและตำแหน่งขององค์ประกอบในหน้าจอแบบเรียบง่าย", "โค้ด HTML ที่เสร็จสมบูรณ์แล้ว", "แผนผังการเดินสายไฟในออฟฟิศ" }, 1)
                },
                new[]
                {
                    ("Visual Hierarchy ในการออกแบบ UI หมายถึงเรื่องใด?", new[] { "การจัดลำดับความสำคัญของสิ่งที่มองเห็น เช่น ขนาด สี ตำแหน่ง เพื่อนำสายตาผู้ใช้", "การใช้เฉดสีเทาทั้งหมดในหน้าเว็บ", "การเขียนฟอนต์ให้มีขนาดเท่ากันทุกตัวอักษร", "การซ่อนปุ่มกดไม่ให้ผู้ใช้มองเห็น" }, 0),
                    ("Usability Testing ควรทำเมื่อใดในกระบวนการพัฒนาผลิตภัณฑ์?", new[] { "ทำเฉพาะเมื่อปล่อยผลิตภัณฑ์สู่ตลาดไปแล้ว 1 ปี", "ทำอย่างสม่ำเสมอตั้งแต่ขั้นตอน Prototype เพื่อหาปัญหาและปรับปรุงก่อนพัฒนาจริง", "ไม่ต้องทำหากทีมออกแบบมั่นใจ", "ทำเฉพาะเมื่อถูกลูกค้าร้องเรียน" }, 1),
                    ("Information Architecture (IA) ในเว็บไซต์ช่วยแก้ปัญหาใด?", new[] { "การจัดหมวดหมู่ข้อมูลและโครงสร้างเมนูให้ผู้ใช้งานค้นหาสิ่งที่ต้องการได้ง่าย", "การเพิ่มความเร็วของฐานข้อมูล", "การคำนวณภาษีมูลค่าเพิ่ม", "การป้องกันไวรัสคอมพิวเตอร์" }, 0),
                    ("กฎ 3 คลิก (3-Click Rule) ในการออกแบบ UX มีแนวคิดอย่างไร?", new[] { "ผู้ใช้ต้องกดปุ่มได้ไม่เกิน 3 ครั้งต่อนาที", "ผู้ใช้ควรสามารถเข้าถึงข้อมูลที่ต้องการได้ภายในไม่เกิน 3 คลิก", "เว็บไซต์ต้องมีหน้าจอเพียง 3 หน้า", "ต้องกรอกรหัสผ่าน 3 รอบ" }, 1)
                }
            ),

            // 13. figma-ui-prototyping
            new(
                "figma-ui-prototyping",
                new[]
                {
                    ("เครื่องมือ Figma ทำงานบนสภาพแวดล้อมใดเป็นหลัก?", new[] { "โปรแกรมบน Windows เท่านั้น", "Cloud-based ทำงานผ่านเว็บเบราว์เซอร์และทำงานร่วมกันแบบ Real-time ได้", "ระบบปฏิบัติการ iOS เท่านั้น", "ต้องติดตั้งโปรแกรมลงในฮาร์ดแวร์เฉพาะ" }, 1),
                    ("Frame ใน Figma เปรียบเสมือนสิ่งใดในการออกแบบ?", new[] { "กระดาษหรือหน้าจออุปกรณ์ที่ใช้บรรจุองค์ประกอบการออกแบบ (เช่น iPhone, Desktop)", "ฟิลเตอร์แต่งรูปภาพ", "เครื่องมือเขียนโค้ด", "แถบเมนูสำหรับเซฟไฟล์" }, 0),
                    ("Component ใน Figma มีประโยชน์เพื่ออะไร?", new[] { "สร้างชิ้นส่วนดีไซน์ที่นำกลับมาใช้ซ้ำได้ และเมื่อแก้ไขตัวแม่ ตัวลูกจะเปลี่ยนตามอัตโนมัติ", "ใช้สำหรับวาดภาพ 3 มิติ", "ใช้ส่งอีเมลหาลูกค้า", "ใช้แทนการเขียน CSS" }, 0)
                },
                new[]
                {
                    ("ฟีเจอร์ Auto Layout ใน Figma ช่วยเรื่องใดในการออกแบบ UI?", new[] { "จัดระยะห่างและปรับขนาดของกล่องหรือปุ่มให้ขยายตามข้อความภายในโดยอัตโนมัติ (คล้าย Flexbox)", "สร้างโค้ด Backend อัตโนมัติ", "แต่งรูปภาพใบหน้าให้เรียบเนียน", "สร้างโลโก้โดยไม่ต้องวาด" }, 0),
                    ("Variants ใน Figma Component ช่วยลดความยุ่งยากในเรื่องใด?", new[] { "การรวมสถานะต่างๆ ของ Component เดียวกัน (เช่น Button: Primary, Hover, Disabled) ไว้ในกลุ่มเดียว", "การลดขนาดของไฟล์งาน", "การแปลภาษาบนหน้าจอ", "การเปลี่ยนสีพื้นหลังของคอมพิวเตอร์" }, 0),
                    ("การทำ Smart Animate ใน Prototype ของ Figma มีประโยชน์อย่างไร?", new[] { "สร้างการเคลื่อนไหวที่ลื่นไหลระหว่างหน้าจอโดยจับคู่ Layer ที่มีชื่อตรงกัน", "บีบอัดวิดีโอเป็นไฟล์ MP4", "สร้างเพลงประกอบหน้าจอ", "ทำให้หน้าเว็บโหลดเร็วขึ้น" }, 0),
                    ("Inspect Tool (Dev Mode) ใน Figma มีไว้สำหรับใครและเพื่อประโยชน์ใด?", new[] { "สำหรับนักพัฒนา เพื่อดูค่า CSS, สี, ระยะห่าง, ขนาด และดาวน์โหลด Assets ไปเขียนโค้ด", "สำหรับผู้บริหารเพื่ออนุมัติงบประมาณ", "สำหรับฝ่ายบุคคลเพื่อเช็กชื่อ", "สำหรับลูกค้าเพื่อสั่งซื้อสินค้า" }, 0)
                }
            ),

            // 14. excel-workplace-skills
            new(
                "excel-workplace-skills",
                new[]
                {
                    ("การเขียนสูตรคำนวณในโปรแกรม Excel ต้องเริ่มต้นด้วยเครื่องหมายใดเสมอ?", new[] { "#", "$", "=", "@" }, 2),
                    ("ฟังก์ชันใดใช้สำหรับหาผลรวมของตัวเลขในแถวหรือคอลัมน์?", new[] { "=AVERAGE()", "=SUM()", "=COUNT()", "=TOTAL()" }, 1),
                    ("ฟังก์ชันใดใช้สำหรับนับจำนวนเซลล์ที่มีตัวเลข?", new[] { "=COUNT()", "=COUNTA()", "=SUM()", "=NUM()" }, 0)
                },
                new[]
                {
                    ("ฟังก์ชัน XLOOKUP ใน Excel รุ่นใหม่มีข้อดีที่เหนือกว่า VLOOKUP ดั้งเดิมอย่างไร?", new[] { "สามารถค้นหาข้อมูลจากขวาไปซ้ายได้ และไม่ต้องกำหนดหมายเลขคอลัมน์แบบตายตัว", "คำนวณได้เฉพาะตัวเลขเท่านั้น", "ทำงานได้โดยไม่ต้องใช้อินเทอร์เน็ต", "ใช้ได้เฉพาะบนเครื่อง Mac" }, 0),
                    ("PivotTable ใน Excel มีวัตถุประสงค์หลักเพื่อสิ่งใด?", new[] { "สรุปผล วิเคราะห์ และจัดกลุ่มข้อมูลปริมาณมากให้เป็นตารางสรุปรายงานในไม่กี่วินาที", "เขียนโปรแกรมเพื่อสร้างเว็บแอป", "วาดกราฟิกแบบเวกเตอร์", "ใช้ส่งข้อความแชตในออฟฟิศ" }, 0),
                    ("สูตร =IF(A1>=50, \"ผ่าน\", \"ไม่ผ่าน\") จะให้ผลลัพธ์เป็นอะไรหากค่าในเซลล์ A1 คือ 75?", new[] { "ไม่ผ่าน", "ผ่าน", "50", "Error" }, 1),
                    ("การใช้ฟังก์ชัน SUMIFS ต่างจากฟังก์ชัน SUMIF อย่างไร?", new[] { "SUMIFS สามารถกำหนดเงื่อนไขในการหาผลรวมได้มากกว่าหนึ่งเงื่อนไข", "SUMIFS คำนวณได้ช้ากว่า", "SUMIFS ใช้ได้เฉพาะตัวเลขทศนิยม", "ไม่มีความแตกต่างกัน" }, 0)
                }
            ),

            // 15. power-bi-dashboard
            new(
                "power-bi-dashboard",
                new[]
                {
                    ("Power BI เป็นเครื่องมือที่พัฒนาโดยบริษัทใดและใช้สำหรับงานด้านใดเป็นหลัก?", new[] { "Google - ออกแบบเว็บไซต์", "Microsoft - การวิเคราะห์ข้อมูลทางธุรกิจและการสร้าง Business Intelligence Dashboard", "Apple - ตัดต่อวิดีโอ", "Adobe - แต่งภาพถ่าย" }, 1),
                    ("เครื่องมือ Power Query ภายใน Power BI มีหน้าที่หลักเพื่อสิ่งใด?", new[] { "นำเข้าข้อมูล ทำความสะอาดข้อมูล (Data Cleansing) และแปลงรูปข้อมูล (Transform)", "ออกแบบโลโก้", "สร้างเสียงเอฟเฟกต์", "พิมพ์เอกสารลงกระดาษ" }, 0),
                    ("ภาษา DAX ใน Power BI ย่อมาจากคำว่าอะไร?", new[] { "Data Analysis Expressions", "Direct Access XML", "Digital Analytics Extension", "Dynamic Application X" }, 0)
                },
                new[]
                {
                    ("ใน Data Modeling รูปแบบความสัมพันธ์แบบ Star Schema ประกอบด้วยตารางสองประเภทใด?", new[] { "Fact Tables (เก็บตัวเลข/ยอดขาย) และ Dimension Tables (เก็บข้อมูลมิติ เช่น ลูกค้า, วันที่)", "Header Tables และ Footer Tables", "Input Tables และ Output Tables", "Excel Tables และ Word Tables" }, 0),
                    ("ความแตกต่างระหว่าง Calculated Column และ Measure ใน Power BI คือข้อใด?", new[] { "Calculated Column คำนวณทีละแถวและกินเนื้อที่ RAM ส่วน Measure คำนวณตามบริบทการกรองบนรายงานแบบไดนามิก", "Measure กินเนื้อที่มากกว่า Calculated Column", "Calculated Column ใช้ได้เฉพาะกับตัวอักษร", "ทั้งสองอย่างทำงานเหมือนกันทุกประการ" }, 0),
                    ("Slicer ใน Power BI Dashboard มีประโยชน์เพื่อทำสิ่งใด?", new[] { "ใช้เป็นตัวกรองข้อมูลบนหน้าจอแบบมีปฏิสัมพันธ์ (Interactive Filter) เช่น เลือกดูตามปีหรือภูมิภาค", "ตัดต่อภาพกราฟิก", "ลบตารางข้อมูลทิ้ง", "ส่งออกรายงานเป็นไฟล์ MP3" }, 0),
                    ("หากต้องการแสดงแนวโน้มยอดขายตามช่วงเวลา ควรเลือกใช้ Visual ชนิดใด?", new[] { "Pie Chart", "Line Chart (กราฟเส้น)", "Card", "Gauge" }, 1)
                }
            ),

            // 16. sql-for-data-analysis
            new(
                "sql-for-data-analysis",
                new[]
                {
                    ("คำสั่ง SQL พื้นฐานใดใช้สำหรับดึงข้อมูลออกจากตาราง?", new[] { "GET", "FETCH", "SELECT", "PULL" }, 2),
                    ("คำสั่ง WHERE ใน SQL ใช้สำหรับทำสิ่งใด?", new[] { "จัดเรียงข้อมูลจากน้อยไปมาก", "กำหนดเงื่อนไขในการกรองแถวข้อมูลที่ต้องการ", "กำหนดกลุ่มข้อมูล", "สร้างตารางใหม่" }, 1),
                    ("ฟังก์ชันสถิติใดใช้สำหรับหาค่าเฉลี่ยของข้อมูลในคอลัมน์?", new[] { "AVG()", "MEAN()", "AVERAGE()", "MEDIAN()" }, 0)
                },
                new[]
                {
                    ("เมื่อใช้คำสั่ง GROUP BY ใน SQL หากต้องการกรองผลลัพธ์หลังจากการรวมกลุ่ม ต้องใช้คำสั่งใด?", new[] { "WHERE", "FILTER", "HAVING", "LIMIT" }, 2),
                    ("Window Function เช่น ROW_NUMBER() OVER (PARTITION BY Department ORDER BY Salary DESC) ทำหน้าที่อะไร?", new[] { "ลบข้อมูลเงินเดือนที่ซ้ำซ้อน", "สร้างลำดับที่ตัวเลขของพนักงานโดยแยกตามแผนกและเรียงตามเงินเดือนจากมากไปน้อย", "หาผลรวมเงินเดือนของทุกคน", "เปลี่ยนชื่อแผนกพนักงาน" }, 1),
                    ("Common Table Expression (CTE) ที่ขึ้นต้นด้วยคำสั่ง WITH มีประโยชน์อย่างไร?", new[] { "ช่วยให้เขียน Query ที่ซับซ้อนให้อยู่ในรูปตารางชั่วคราวที่อ่านง่ายและนำมาอ้างอิงซ้ำได้", "ทำให้ลบฐานข้อมูลได้เร็วขึ้น", "ใช้สำหรับสร้าง Index แบบถาวร", "ใช้สำหรับส่งข้อมูลผ่านอีเมล" }, 0),
                    ("การทำ LEFT JOIN ระหว่างตาราง Orders (ซ้าย) กับ Customers (ขวา) จะได้ผลลัพธ์อย่างไร?", new[] { "ได้เฉพาะแถวที่มีข้อมูลตรงกันทั้งสองฝั่ง", "ได้คำสั่งซื้อทั้งหมดฝั่งซ้าย แม้ว่าลูกค้าฝั่งขวาจะไม่มีข้อมูลก็ตาม (ค่าจะเป็น NULL)", "ได้เฉพาะลูกค้าที่ไม่มีคำสั่งซื้อ", "ได้ข้อมูลว่างเปล่าเสมอ" }, 1)
                }
            ),

            // 17. cybersecurity-awareness
            new(
                "cybersecurity-awareness",
                new[]
                {
                    ("Phishing คือภัยคุกคามทางไซเบอร์ในลักษณะใด?", new[] { "การทำลายฮาร์ดแวร์ด้วยความร้อนสูง", "การหลอกลวงผ่านอีเมลหรือข้อความปลอมเพื่อขโมยรหัสผ่านหรือข้อมูลทางการเงิน", "การลืมรหัสผ่านของตนเอง", "การเชื่อมต่ออินเทอร์เน็ตที่ล่าช้า" }, 1),
                    ("Ransomware ส่งผลกระทบต่อเครื่องเหยื่ออย่างไร?", new[] { "แสดงโฆษณาบนหน้าจอ", "เข้ารหัสไฟล์ทั้งหมดในเครื่องและเรียกค่าไถ่เพื่อแลกกับกุญแจถอดรหัส", "ทำให้คีย์บอร์ดพิมพ์ไม่ได้", "ลบไฟล์รูปภาพทิ้งเพียงไฟล์เดียว" }, 1),
                    ("การเปิดใช้งาน Multi-Factor Authentication (MFA) ช่วยเพิ่มความปลอดภัยอย่างไร?", new[] { "ทำให้ไม่ต้องจำรหัสผ่านอีกต่อไป", "เพิ่มขั้นตอนยืนยันตัวตนชั้นที่ 2 (เช่น OTP หรือ Authenticator App) ทำให้ยากต่อการถูกแฮก", "ทำให้อินเทอร์เน็ตเร็วขึ้น", "ช่วยป้องกันฝุ่นเข้าเครื่องคอมพิวเตอร์" }, 1)
                },
                new[]
                {
                    ("เมื่อได้รับอีเมลแจ้งเตือนว่า 'บัญชีธนาคารของคุณถูกระงับ กรุณากดลิงก์นี้เพื่อยืนยันตัวตนทันที' ข้อใดคือแนวปฏิบัติที่ปลอดภัยที่สุด?", new[] { "รีบกดลิงก์และกรอกรหัสผ่านทันทีเพื่อความปลอดภัย", "ตรวจสอบที่อยู่อีเมลผู้ส่ง อย่ากดลิงก์ในอีเมล และโทรศัพท์ตรวจสอบกับธนาคารโดยตรง", "ส่งต่ออีเมลนี้ให้เพื่อนร่วมงานทุกคนในบริษัท", "ตอบกลับอีเมลพร้อมแนบภาพบัตรประชาชน" }, 1),
                    ("การเชื่อมต่อ Wi-Fi สาธารณะตามร้านกาแฟที่ไม่มีรหัสผ่าน มีความเสี่ยงต่อการถูกดักจับข้อมูลประเภทใดมากที่สุด?", new[] { "Man-in-the-Middle (MitM) Attack", "SQL Injection", "Cross-Site Scripting", "Physical Theft" }, 0),
                    ("Social Engineering ในวงการความปลอดภัยไซเบอร์หมายถึงสิ่งใด?", new[] { "การเขียนโปรแกรมบนโซเชียลมีเดีย", "การใช้จิตวิทยาและเทคนิคหลอกล่อให้มนุษย์เปิดเผยข้อมูลลับหรือรหัสผ่าน", "การศึกษาพฤติกรรมของสัตว์", "การซ่อมบำรุงเครือข่ายอินเทอร์เน็ต" }, 1),
                    ("แนวปฏิบัติที่ดีที่สุดในการจัดการรหัสผ่านในองค์กรคือข้อใด?", new[] { "จดรหัสผ่านใส่กระดาษแปะไว้ใต้คีย์บอร์ด", "ใช้รหัสผ่านเดียวกันในทุกระบบเพื่อป้องกันการลืม", "ใช้ Password Manager ช่วยสร้างและจัดเก็บรหัสผ่านที่ซับซ้อนและแตกต่างกันในแต่ละบริการ", "ใช้ชื่อวันเกิดเป็นรหัสผ่านเสมอ" }, 2)
                }
            ),

            // 18. cloud-azure-fundamentals
            new(
                "cloud-azure-fundamentals",
                new[]
                {
                    ("บริการคลาวด์แบบ IaaS (Infrastructure as a Service) ให้อะไรแก่ผู้ใช้งาน?", new[] { "ซอฟต์แวร์สำเร็จรูปพร้อมใช้งานผ่านเว็บ เช่น Office 365", "โครงสร้างพื้นฐานเสมือน เช่น Virtual Machines, เครือข่าย และสตอเรจ โดยผู้ใช้ดูแล OS เอง", "แพลตฟอร์มรันโค้ดโดยไม่ต้องสนใจเซิร์ฟเวอร์", "ฮาร์ดแวร์ทางกายภาพส่งมาที่บ้าน" }, 1),
                    ("Azure Region ในระบบคลาวด์ของ Microsoft หมายถึงสิ่งใด?", new[] { "ประเทศที่มีสำนักงานขายของ Microsoft", "พื้นที่ทางภูมิศาสตร์ที่ประกอบด้วยศูนย์ข้อมูล (Datacenters) เชื่อมต่อกันด้วยเครือข่ายความเร็วสูง", "ชื่อภาษาโปรแกรม", "ประเภทของฐานข้อมูล" }, 1),
                    ("ข้อใดคือประโยชน์ของ Cloud Elasticity?", new[] { "ความสามารถในการเพิ่มหรือลดทรัพยากรประมวลผลได้ตามความต้องการใช้งานจริงอัตโนมัติ", "การใช้อุปกรณ์มือถือเชื่อมต่อคลาวด์", "การจ่ายเงินรายปีแบบคงที่เสมอ", "การไม่จำเป็นต้องเชื่อมต่ออินเทอร์เน็ต" }, 0)
                },
                new[]
                {
                    ("Azure App Service จัดเป็นบริการคลาวด์ในโมเดลประเภทใด?", new[] { "IaaS (Infrastructure as a Service)", "PaaS (Platform as a Service)", "SaaS (Software as a Service)", "BaaS (Backend as a Service)" }, 1),
                    ("Azure Availability Zones (AZ) ถูกออกแบบมาเพื่อแก้ปัญหาใดเป็นหลัก?", new[] { "ช่วยให้ระบบทำงานได้อย่างต่อเนื่องแม้ศูนย์ข้อมูลแห่งใดแห่งหนึ่งจะเกิดไฟฟ้าดับหรือภัยธรรมชาติ", "ช่วยลดราคาค่าบริการลง 90%", "ช่วยให้เขียนโปรแกรมภาษา C# ได้เร็วขึ้น", "ช่วยเพิ่มจำนวนหน้าจอคอมพิวเตอร์" }, 0),
                    ("Role-Based Access Control (Azure RBAC) มีบทบาทสำคัญในเรื่องใด?", new[] { "การกำหนดสิทธิ์ในการเข้าถึงทรัพยากรบน Azure ตามบทบาทหน้าที่ของบุคคลหรือแอปพลิเคชัน (หลักการ Least Privilege)", "การคำนวณภาษี", "การจัดหมวดหมู่อีเมล", "การดาวน์โหลดวิดีโอ" }, 0),
                    ("เครื่องมือใดใช้สำหรับตั้งค่างบประมาณและการแจ้งเตือนค่าใช้จ่ายบน Azure?", new[] { "Azure Monitor", "Azure Cost Management & Budgets", "Azure Policy", "Azure Advisor" }, 1)
                }
            ),

            // 19. docker-for-developers
            new(
                "docker-for-developers",
                new[]
                {
                    ("Container แตกต่างจาก Virtual Machine (VM) อย่างไรในเชิงสถาปัตยกรรม?", new[] { "Container ใช้งาน Guest OS ของตนเองอย่างสมบูรณ์", "Container ใช้ Kernel ของ Host OS ร่วมกัน ทำให้มีขนาดเล็กและเริ่มทำงานได้เร็วกว่า VM อย่างมาก", "Container รันได้เฉพาะบนระบบปฏิบัติการ Windows เท่านั้น", "ไม่มีความแตกต่างกัน" }, 1),
                    ("Docker Image มีสถานะเปรียบเสมือนสิ่งใดในการเขียนโปรแกรม?", new[] { "ตัวแปรชั่วคราว", "พิมพ์เขียวหรือแม่แบบ (Template) ที่ใช้สำหรับสร้าง Container", "ระบบปฏิบัติการหลักของเครื่อง", "สายเคเบิลเชื่อมต่ออินเทอร์เน็ต" }, 1),
                    ("คำสั่งใดใช้สำหรับดูรายการ Containers ทั้งหมดที่กำลังรันอยู่ในเครื่อง?", new[] { "docker images", "docker ps", "docker run", "docker build" }, 1)
                },
                new[]
                {
                    ("คำสั่งใดใน Dockerfile ใช้สำหรับกำหนดคำสั่งหลักที่จะถูกประมวลผลเมื่อ Container เริ่มต้นทำงาน?", new[] { "RUN", "COPY", "ENTRYPOINT / CMD", "EXPOSE" }, 2),
                    ("เมื่อต้องการเก็บรักษาข้อมูลของฐานข้อมูล (เช่น ข้อมูลใน SQL Server) ไม่ให้สูญหายเมื่อลบ Container ต้องใช้สิ่งใด?", new[] { "Docker Network", "Docker Volumes / Bind Mounts", "Docker Commit", "Docker Tag" }, 1),
                    ("ไฟล์ docker-compose.yml ถูกนำมาใช้งานเมื่อใด?", new[] { "เมื่อต้องการนิยามและรันระบบที่ประกอบด้วยหลาย Containers (Multi-container) พร้อมกันด้วยคำสั่งเดียว", "เมื่อต้องการติดตั้งโปรแกรม Docker Desktop", "เมื่อต้องการแก้ไขโค้ดภาษา HTML", "เมื่อต้องการตั้งรหัสผ่านให้คอมพิวเตอร์" }, 0),
                    ("คำสั่ง 'EXPOSE 80' ใน Dockerfile มีหน้าที่อะไร?", new[] { "เปิดพอร์ต 80 ของเราเตอร์ภายนอกทันที", "เป็นการบันทึกเอกสาร (Documentation) เพื่อแจ้งว่า Container นี้จะให้บริการบนพอร์ต 80", "สั่งให้ Container ปิดการเชื่อมต่อ", "ลบพอร์ต 80 ออกจากระบบ" }, 1)
                }
            ),

            // 20. software-testing-unit-tests
            new(
                "software-testing-unit-tests",
                new[]
                {
                    ("Unit Test มีขอบเขตการทดสอบที่เน้นในส่วนใดของระบบ?", new[] { "ทดสอบระบบทั้งหมดตั้งแต่หน้าเว็บจนถึงฐานข้อมูล", "ทดสอบหน่วยย่อยที่สุดของโค้ด เช่น ฟังก์ชันหรือเมธอดหนึ่งๆ โดยแยกออกจากภายนอก", "ทดสอบความเร็วของสัญญาณอินเทอร์เน็ต", "ทดสอบการพิมพ์งานบนคีย์บอร์ด" }, 1),
                    ("โครงสร้าง AAA ในการเขียน Unit Test ประกอบด้วยขั้นตอนใดบ้าง?", new[] { "Add, Alter, Apply", "Arrange (เตรียมข้อมูล), Act (เรียกทำงาน), Assert (ตรวจสอบผลลัพธ์)", "Always, Ask, Answer", "Analyze, Attack, Assess" }, 1),
                    ("เครื่องมือยอดนิยมสำหรับการเขียน Unit Test บนแพลตฟอร์ม .NET คือข้อใด?", new[] { "xUnit / NUnit", "React", "Docker", "Bootstrap" }, 0)
                },
                new[]
                {
                    ("การทำ Mocking ในการเขียน Unit Test มีประโยชน์หลักอย่างไร?", new[] { "การจำลองการทำงานของ Dependencies ภายนอก (เช่น Database, API) เพื่อให้ทดสอบโค้ดเฉพาะจุดได้อย่างรวดเร็วและควบคุมได้", "การลบโค้ดส่วนเกินทิ้ง", "การเพิ่มเวลาในการรัน Test", "การแปลภาษาโปรแกรม" }, 0),
                    ("Code Coverage ในการทดสอบซอฟต์แวร์คือตัวชี้วัดเรื่องใด?", new[] { "จำนวนบรรทัดของโค้ดทั้งหมดในโปรเจกต์", "เปอร์เซ็นต์ของโค้ดที่ถูกทดสอบครอบคลุมโดยชุดคำสั่ง Test Suite", "ความเร็วในการประมวลผลของ CPU", "จำนวนผู้ใช้งานระบบพร้อมกัน" }, 1),
                    ("ข้อใดเป็นคุณสมบัติของ Unit Test ที่ดีตามหลัก FIRST?", new[] { "รันช้าและต้องพึ่งพาข้อมูลจริงเสมอ", "Fast (รวดเร็ว), Independent (ไม่ขึ้นต่อกัน), Repeatable (ได้ผลซ้ำเดิม), Self-validating, Timely", "เขียนยากและแก้ไขไม่ได้", "รันได้เฉพาะบนเครื่องเซิร์ฟเวอร์เท่านั้น" }, 1),
                    ("Assert.Equal(expected, actual) มีหน้าที่อะไรในเคสทดสอบ?", new[] { "กำหนดค่าตัวแปรใหม่", "ตรวจสอบว่าผลลัพธ์ที่ได้จากโปรแกรม (actual) มีค่าตรงกับที่คาดหวังไว้ (expected) หรือไม่", "พิมพ์ข้อความออกทางหน้าจอ", "จบการทำงานของโปรแกรม" }, 1)
                }
            ),

            // 21. github-actions-ci-basics
            new(
                "github-actions-ci-basics",
                new[]
                {
                    ("GitHub Actions เป็นเครื่องมือสำหรับงานประเภทใด?", new[] { "สร้างภาพกราฟิก", "ทำ Continuous Integration และ Continuous Delivery (CI/CD) แบบอัตโนมัติบน GitHub", "เขียนเอกสารสรุปการประชุม", "จัดการฐานข้อมูลบนเครื่องตนเอง" }, 1),
                    ("ไฟล์คอนฟิก Workflow ของ GitHub Actions ต้องบันทึกไว้ในโฟลเดอร์ใดและมีนามสกุลไฟล์อะไร?", new[] { "config/workflow.json", ".github/workflows/*.yml หรือ *.yaml", "scripts/deploy.sh", ".vscode/tasks.json" }, 1),
                    ("Event ใดใน GitHub Actions ใช้สั่งให้ Workflow ทำงานเมื่อมีการดันโค้ดขึ้นสู่ Repository?", new[] { "on: push", "on: click", "on: download", "on: delete" }, 0)
                },
                new[]
                {
                    ("Continuous Integration (CI) ช่วยให้ทีมพัฒนาลดปัญหาเรื่องใดเป็นหลัก?", new[] { "ช่วยให้ตรวจพบข้อผิดพลาดของโค้ดและข้อขัดแย้ง (Merge Conflicts) ได้อย่างรวดเร็วตั้งแต่เนิ่นๆ", "ช่วยลดการจ่ายค่าอินเทอร์เน็ต", "ช่วยให้ไม่ต้องเขียน Unit Test", "ช่วยให้ออกแบบหน้าจอได้สวยงามขึ้น" }, 0),
                    ("ใน GitHub Actions หากต้องการเก็บค่าความลับ เช่น Connection String หรือ API Key ควรจัดเก็บที่ใด?", new[] { "เขียนใส่ในไฟล์ YAML โดยตรง", "บันทึกใน GitHub Repository Secrets แล้วเรียกใช้ผ่าน ${{ secrets.SECRET_NAME }}", "ใส่ไว้ใน Commit Message", "สร้างโฟลเดอร์ชื่อ password" }, 1),
                    ("Runner ในสถาปัตยกรรม GitHub Actions หมายถึงสิ่งใด?", new[] { "นักพัฒนาที่กำลังพิมพ์โค้ด", "เซิร์ฟเวอร์เสมือน (เช่น ubuntu-latest, windows-latest) ที่ทำหน้าที่ประมวลผลคำสั่งในแต่ละ Job", "คำสั่งสำหรับปิดโปรแกรม", "ปุ่มกดยืนยันการทำงาน" }, 1),
                    ("คำสั่ง 'actions/checkout@v4' ใน Workflow มีหน้าที่อะไร?", new[] { "ดาวน์โหลดโค้ดจาก Repository มายังเครื่อง Runner เพื่อให้ขั้นตอนถัดไปสามารถเข้าถึงไฟล์ได้", "ตรวจสอบยอดเงินในบัญชี", "ออกจากระบบ GitHub", "ลบ Repository ทิ้ง" }, 0)
                }
            ),

            // 22. agile-scrum-practical
            new(
                "agile-scrum-practical",
                new[]
                {
                    ("Sprint ในกรอบการทำงาน Scrum หมายถึงสิ่งใด?", new[] { "การแข่งขันเขียนโค้ดที่เร็วที่สุด", "กรอบระยะเวลาคงที่ (Time-box เช่น 2-4 สัปดาห์) ที่ทีมมุ่งมั่นส่งมอบผลงานที่ใช้งานได้จริง", "การทดสอบระบบก่อนส่งมอบ", "เอกสารสัญญาจ้างงาน" }, 1),
                    ("ใครในทีม Scrum มีหน้าที่รับผิดชอบในการจัดลำดับความสำคัญของ Product Backlog?", new[] { "Scrum Master", "Product Owner (PO)", "Lead Developer", "QA Tester" }, 1),
                    ("Daily Scrum คือกิจกรรมที่มีวัตถุประสงค์เพื่อสิ่งใดและควรใช้เวลาเท่าใด?", new[] { "ประชุมหารือปัญหาเชิงลึกเป็นเวลา 3 ชั่วโมง", "การประชุมสั้นๆ 15 นาที เพื่อตรวจสอบความคืบหน้าของเป้าหมาย Sprint และระบุอุปสรรค", "การรายงานต่อฝ่ายบริหาร", "การจัดงานเลี้ยงฉลองทีม" }, 1)
                },
                new[]
                {
                    ("กิจกรรม Sprint Retrospective มีเป้าหมายสำคัญเพื่อสิ่งใด?", new[] { "การหาคนผิดในทีมเมื่อเกิดข้อผิดพลาด", "การทบทวนกระบวนการทำงาน การสื่อสาร และเครื่องมือ เพื่อหาแนวทางปรับปรุงใน Sprint ถัดไป", "การคำนวณเงินโบนัสประจำปี", "การแสดงผลงานแก่ลูกค้าภายนอก" }, 1),
                    ("โครงสร้างการเขียน User Story นิยมใช้รูปแบบใดเพื่อให้เข้าใจความต้องการชัดเจน?", new[] { "As a [ผู้ใช้], I want [สิ่งที่ต้องการ], So that [ประโยชน์ที่ได้รับ]", "If [เงื่อนไข] Then [ผลลัพธ์] Else [ข้อยกเว้น]", "Title, Description, Date, Signature", "Step 1, Step 2, Step 3" }, 0),
                    ("Story Points ในการประมาณการงานแบบ Agile ใช้แทนสิ่งใดเป็นหลัก?", new[] { "จำนวนชั่วโมงเป๊ะๆ ในการทำงาน", "ขนาด ความซับซ้อน และความไม่แน่นอนของงาน โดยเปรียบเทียบเชิงสัมพัทธ์", "จำนวนบรรทัดของโค้ดที่จะเขียน", "จำนวนเงินงบประมาณที่ต้องใช้" }, 1),
                    ("Definition of Done (DoD) มีความสำคัญอย่างไรในทีม Scrum?", new[] { "รายการตรวจสอบที่เป็นข้อตกลงร่วมกันว่าเมื่อใดงานจึงจะถือว่าเสร็จสมบูรณ์และพร้อมส่งมอบ", "วันหมดอายุของโปรเจกต์", "เกณฑ์การผ่านโปรของพนักงานใหม่", "รายการอุปกรณ์คอมพิวเตอร์ที่ต้องซื้อ" }, 0)
                }
            ),

            // 23. tech-communication-presentation
            new(
                "tech-communication-presentation",
                new[]
                {
                    ("หลักการสำคัญที่สุดในการอธิบายเรื่องเชิงเทคนิคให้กับบุคคลที่ไม่ใช่สายเทค (Non-tech) คือข้อใด?", new[] { "ใช้คำศัพท์เทคนิคภาษาอังกฤษให้มากที่สุดเพื่อแสดงความเชี่ยวชาญ", "เน้นที่ผลลัพธ์ คุณค่าทางธุรกิจ และใช้คำอุปมาเปรียบเทียบที่เห็นภาพง่าย", "ปฏิเสธการพูดคุยและส่งเอกสารให้อ่านเอง", "พูดให้เร็วที่สุดเพื่อประหยัดเวลา" }, 1),
                    ("การเขียน Technical Documentation ที่ดีควรคำนึงถึงสิ่งใดเป็นอันดับแรก?", new[] { "ผู้อ่านคือใคร และต้องการทำอะไรให้สำเร็จหลังจากอ่านเอกสารนี้", "จำนวนหน้าต้องมากกว่า 100 หน้า", "ใช้ฟอนต์ตัวเขียนที่สวยงาม", "ต้องมีโค้ดภาษา C# เสมอ" }, 0),
                    ("การเดโมซอฟต์แวร์ (System Demo) ที่ทรงพลังควรเริ่มต้นอย่างไร?", new[] { "เปิดโค้ดบรรทัดที่ 1 ให้อ่านทีละบรรทัด", "เกริ่นนำปัญหาของผู้ใช้งานจริง แล้วแสดงให้เห็นว่าระบบช่วยแก้ปัญหานั้นได้อย่างไร", "บ่นถึงความเหน็ดเหนื่อยในการพัฒนา", "เริ่มจากการขอโทษล่วงหน้าหากระบบมีบั๊ก" }, 1)
                },
                new[]
                {
                    ("เมื่อได้รับข้อท้วงติงในระหว่างการพรีเซนต์งาน แนวทางปฏิบัติที่แสดงถึงความเป็นมืออาชีพคือข้อใด?", new[] { "โต้เถียงทันทีด้วยอารมณ์เพื่อปกป้องผลงาน", "ตั้งใจรับฟัง สรุปประเด็นซ้ำเพื่อความเข้าใจที่ตรงกัน และตอบอย่างมีหลักการ", "เดินออกจากห้องประชุม", "เงียบและไม่ตอบคำถามใดๆ" }, 1),
                    ("สไลด์นำเสนอผลงานทางเทคโนโลยีที่ดีควรมีลักษณะอย่างไร?", new[] { "อัดแน่นด้วยข้อความและตารางตัวเลขขนาดเล็ก", "มีความสะอาดตา มีลำดับการเล่าเรื่อง ใช้ภาพหรือไดอะแกรมช่วยสื่อความหมาย และเน้นประเด็นสำคัญ", "มีแอนิเมชันกระโดดไปมาตลอดเวลา", "มีเฉพาะภาพถ่ายส่วนตัว" }, 1),
                    ("ในการประชุม Daily Standup ข้อมูล 3 ข้อหลักที่ควรสื่อสารให้กระชับคือข้อใด?", new[] { "เมื่อวานทำอะไร วันนี้จะทำอะไร และมีอุปสรรคใดที่ต้องการความช่วยเหลือหรือไม่", "ปัญหาการเมือง ความคิดเห็นเรื่องอาหารกลางวัน และสภาพอากาศ", "การเงินส่วนบุคคล สุขภาพ และเรื่องส่วนตัว", "รายชื่อลูกค้าที่ติดต่อยาก" }, 0),
                    ("การเขียน Release Notes ที่ดีควรมีประโยชน์ต่อกลุ่มใด?", new[] { "ทั้งผู้ใช้งานทั่วไป (รู้ว่ามีฟีเจอร์ใหม่อะไรหรือแก้อะไร) และทีมเทคนิค (รู้ผลกระทบเชิงระบบ)", "เฉพาะผู้เขียนโปรแกรมคนเดียว", "ฝ่ายบัญชีเท่านั้น", "ไม่ต้องเขียนเนื่องจากไม่มีใครอ่าน" }, 0)
                }
            ),

            // 24. time-management-deep-work
            new(
                "time-management-deep-work",
                new[]
                {
                    ("แนวคิด 'Deep Work' ตามทฤษฎีของ Cal Newport หมายถึงสิ่งใด?", new[] { "การทำงานล่วงเวลาตอนดึกเพื่อเอางานให้เสร็จ", "การมีสมาธิจดจ่อกับงานที่ใช้ความคิดสร้างสรรค์สูงเป็นเวลานานโดยปราศจากสิ่งรบกวน", "การทำงานใต้ผิวน้ำ", "การตอบอีเมลและแชตให้เร็วที่สุดตลอดวัน" }, 1),
                    ("เมทริกซ์การจัดการเวลา Eisenhower Matrix แบ่งงานออกเป็น 4 ช่องตามเกณฑ์ใด?", new[] { "ความยาก และความง่าย", "ความสำคัญ (Importance) และความเร่งด่วน (Urgency)", "ความชอบ และความไม่ชอบ", "ค่าจ้าง และต้นทุน" }, 1),
                    ("เทคนิค Pomodoro คือวิธีการทำงานในลักษณะใด?", new[] { "ทำงานต่อเนื่อง 8 ชั่วโมงโดยไม่หยุดพัก", "ทำงานโฟกัส 25 นาที และพักสั้นๆ 5 นาทีเพื่อรักษาระดับพลังงานของสมอง", "การรับประทานมะเขือเทศขณะทำงาน", "การทำงานเฉพาะช่วงเวลากลางคืน" }, 1)
                },
                new[]
                {
                    ("สิ่งรบกวนแบบใดที่สร้างความเสียหายต่อประสิทธิภาพของนักพัฒนามากที่สุด (Context Switching)?", new[] { "การเปิดเพลงคลอเบาๆ", "การสลับงานไปมาและการตอบการแจ้งเตือนแชต/อีเมลตลอดเวลา ทำให้สมองต้องใช้เวลาเริ่มจูนสมาธิใหม่", "การดื่มน้ำระหว่างวัน", "การนั่งเก้าอี้ที่สบายเกินไป" }, 1),
                    ("Time Blocking มีหลักการทำงานอย่างไรในการวางแผนแต่ละวัน?", new[] { "การจัดสรรช่วงเวลาในปฏิทินแบบชัดเจนสำหรับภารกิจเฉพาะ เพื่อให้มั่นใจว่างานสำคัญมีเวลาทำแน่นอน", "การลบปฏิทินทิ้ง", "การรอให้มีอารมณ์แล้วค่อยทำงาน", "การทำงานตามลำดับใครทักมาก่อนทำก่อน" }, 0),
                    ("งานประเภท Shallow Work (เช่น ตอบอีเมลทั่วไป, กรอกแบบฟอร์มประจำวัน) ควรจัดการอย่างไรเพื่อไม่ให้กินเวลา Deep Work?", new[] { "จัดกลุ่มทำพร้อมกันในช่วงเวลาเฉพาะ (Batching) เช่น ช่วงบ่าย 30 นาที แทนที่จะทำประปรายทั้งวัน", "ไม่ทำเลยตลอดชีวิต", "ทำเป็นสิ่งแรกเมื่อตื่นนอนและทำทั้งวัน", "จ้างคนอื่นทำทั้งหมด" }, 0)
                }
            ),

            // 25. personal-finance-budgeting
            new(
                "personal-finance-budgeting",
                new[]
                {
                    ("ตามกฎการจัดสรรงบประมาณ 50/30/20 สัดส่วน 20% ควรจัดสรรสำหรับเรื่องใด?", new[] { "ค่าใช้จ่ายจำเป็น (Needs)", "ความต้องการและสันทนาการ (Wants)", "การออม การลงทุน และการชำระหนี้สิน (Savings/Debt Repayment)", "การบริจาค" }, 2),
                    ("เงินสำรองฉุกเฉิน (Emergency Fund) ที่ปลอดภัยควรมีมูลค่าเทียบเท่าค่าใช้จ่ายกี่เดือน?", new[] { "1 สัปดาห์", "3 ถึง 6 เดือนของค่าใช้จ่ายจำเป็นต่อเดือน", "10 ปี", "ไม่จำเป็นต้องมี" }, 1),
                    ("ดอกเบี้ยทบต้น (Compound Interest) มีหลักการทำงานสำคัญอย่างไร?", new[] { "ดอกเบี้ยจะลดลงเรื่อยๆ ตามเวลา", "ผลตอบแทนหรือดอกเบี้ยที่ได้รับจะถูกนำไปรวมกับเงินต้นเพื่อสร้างผลตอบแทนก้อนใหม่ต่อไปเรื่อยๆ", "คิดเฉพาะเงินต้นเริ่มต้นเท่านั้น", "ได้เฉพาะผู้สูงอายุเท่านั้น" }, 1)
                },
                new[]
                {
                    ("หนี้ประเภทใดจัดเป็น 'หนี้ดี' (Good Debt) ในมุมมองทางการเงินส่วนบุคคล?", new[] { "หนี้ที่สร้างมูลค่าเพิ่มหรือสร้างรายได้ในอนาคต เช่น กู้ซื้อที่อยู่อาศัย หรือกู้เพื่อการศึกษาที่ดอกเบี้ยสมเหตุสมผล", "หนี้บัตรเครดิตที่จ่ายขั้นต่ำ", "หนี้นอกระบบดอกเบี้ยร้อยละยี่สิบ", "หนี้จากการพนันออนไลน์" }, 0),
                    ("การทำบัญชีรายรับ-รายจ่ายมีประโยชน์หลักอย่างไรต่อสุขภาพทางการเงิน?", new[] { "ช่วยให้มองเห็นรอยรั่วไหลของเงินที่ไม่จำเป็น และสามารถปรับเปลี่ยนพฤติกรรมการใช้จ่ายได้อย่างมีทิศทาง", "ทำให้เงินในกระเป๋าเพิ่มขึ้นทันทีโดยไม่ต้องทำงาน", "ใช้สำหรับยื่นกู้เงินได้ไม่จำกัด", "ทำให้ได้รับการยกเว้นภาษีทุกประเภท" }, 0),
                    ("การลงทุนแบบ Dollar-Cost Averaging (DCA) คืออะไร?", new[] { "การทุ่มเงินทั้งหมดซื้อสินทรัพย์ในวันที่ราคาต่ำสุด", "การลงทุนด้วยจำนวนเงินคงที่อย่างสม่ำเสมอในทุกช่วงเวลา (เช่น ทุกเดือน) โดยไม่คำนึงถึงความผันผวนของราคา", "การเก็งกำไรระยะสั้นใน 1 วัน", "การฝากเงินในตู้เซฟที่บ้าน" }, 1),
                    ("อัตราเงินเฟ้อ (Inflation) ส่งผลกระทบต่อเงินออมที่ฝากไว้เฉยๆ ในธนาคารอย่างไร?", new[] { "ทำให้เงินมีมูลค่าเพิ่มขึ้น", "ทำให้อำนาจซื้อที่แท้จริงของเงินลดลงตามเวลา หากผลตอบแทนดอกเบี้ยต่ำกว่าอัตราเงินเฟ้อ", "ไม่มีผลกระทบใดๆ", "ทำให้ยอดเงินในสมุดบัญชีหายไป" }, 1)
                }
            ),

            // 26. digital-marketing-small-business
            new(
                "digital-marketing-small-business",
                new[]
                {
                    ("SEO ในการตลาดดิจิทัลย่อมาจากคำว่าอะไรและมีเป้าหมายเพื่อสิ่งใด?", new[] { "Social Engine Online - การโปรโมตเพจ", "Search Engine Optimization - การปรับแต่งเว็บไซต์ให้ติดอันดับต้นๆ บนผลการค้นหาธรรมชาติของ Google", "Sales Executive Officer - ตำแหน่งพนักงานขาย", "Simple Email Operation - การส่งอีเมล" }, 1),
                    ("Customer Persona คืออะไรในกระบวนการวางแผนการตลาด?", new[] { "รายชื่อลูกค้าทั้งหมดในระบบ", "การสร้างภาพโปรไฟล์ตัวแทนลูกค้าในอุดมคติ โดยระบุพฤติกรรม ปัญหา ความสนใจ และข้อมูลประชากร", "รูปถ่ายหน้าร้าน", "ยอดขายรวมต่อปี" }, 1),
                    ("ตัวชี้วัด CTR (Click-Through Rate) หมายถึงอัตราส่วนของสิ่งใด?", new[] { "จำนวนคนที่คลิกโฆษณา เทียบกับจำนวนคนที่เห็นโฆษณานั้น (Impressions)", "จำนวนเงินที่จ่ายค่าโฆษณา", "จำนวนสินค้าที่ถูกส่งคืน", "เปอร์เซ็นต์กำไรสุทธิ" }, 0)
                },
                new[]
                {
                    ("AIDA Model ในการวางแผนโฆษณาและการตลาดประกอบด้วย 4 ขั้นตอนใด?", new[] { "Action, Input, Data, Analysis", "Attention (ดึงดูดสายตา), Interest (สร้างความสนใจ), Desire (กระตุ้นความอยากได้), Action (ปิดการขาย)", "Ask, Invest, Deliver, Accept", "Align, Identify, Design, Approve" }, 1),
                    ("ความแตกต่างระหว่าง Organic Reach และ Paid Reach บนโซเชียลมีเดียคือข้อใด?", new[] { "Organic เกิดจากการเข้าถึงตามธรรมชาติโดยไม่เสียเงิน ส่วน Paid เกิดจากการซื้อโฆษณาเพื่อขยายการเข้าถึง", "Organic ต้องจ่ายเงินแพงกว่า", "Paid ใช้ได้เฉพาะกับคอมพิวเตอร์", "ทั้งสองอย่างมีความหมายเหมือนกัน" }, 0),
                    ("Call to Action (CTA) บนหน้าเว็บไซต์หรือโพสต์โฆษณาควรมีลักษณะอย่างไร?", new[] { "ชัดเจน โดดเด่น และบอกผู้ใช้ตรงๆ ว่าต้องการให้ทำอะไรต่อไป เช่น 'สมัครเรียนฟรีวันนี้'", "ซ่อนไว้ที่มุมล่างสุดของหน้า", "มีข้อความกำกวมเพื่อสร้างความลึกลับ", "ไม่ควรมี CTA เพื่อความสุภาพ" }, 0),
                    ("Retargeting / Remarketing ในการยิงโฆษณาดิจิทัลมีหลักการทำงานอย่างไร?", new[] { "การแสดงโฆษณาซ้ำแก่ผู้ที่เคยเข้าชมเว็บไซต์หรือมีปฏิสัมพันธ์กับแบรนด์มาก่อน เพื่อกระตุ้นให้กลับมาซื้อสินค้า", "การยิงโฆษณาไปยังคนที่ไม่เคยใช้อินเทอร์เน็ต", "การเปลี่ยนรูปภาพสินค้าทุกวัน", "การลบประวัติการเข้าชมเว็บ" }, 0)
                }
            ),

            // 27. content-writing-clear-copy
            new(
                "content-writing-clear-copy",
                new[]
                {
                    ("พาดหัว (Headline) ที่ดีมีบทบาทสำคัญที่สุดในข้อใด?", new[] { "เป็นส่วนที่หยุดสายตาผู้อ่านและจูงใจให้เปิดอ่านเนื้อหาต่อไป", "ใช้สำหรับลงชื่อผู้เขียน", "ทำให้หน้ากระดาษเต็ม", "เป็นข้อกำหนดของกฎหมาย" }, 0),
                    ("หลักการเขียนแบบ 'Inverted Pyramid' (พีระมิดหัวกลับ) มีโครงสร้างอย่างไร?", new[] { "เอาเรื่องไม่สำคัญขึ้นก่อน แล้วค่อยสรุปประเด็นหลักท้ายสุด", "นำเสนอข้อมูลที่สำคัญที่สุดและสรุปประเด็นหลักไว้ตอนต้น แล้วตามด้วยรายละเอียดสนับสนุน", "เขียนเป็นรูปสามเหลี่ยมเรขาคณิต", "เขียนย่อหน้าละ 1 คำ" }, 1),
                    ("Brand Voice ในการเขียนคอนเทนต์หมายถึงสิ่งใด?", new[] { "ระดับเสียงที่ใช้อัดวิดีโอ", "บุคลิกภาพ น้ำเสียง และโทนภาษาที่สะท้อนตัวตนและค่านิยมของแบรนด์อย่างสม่ำเสมอ", "การเปิดเพลงคลอระหว่างอ่าน", "การใช้คำศัพท์วัยรุ่นเสมอ" }, 1)
                },
                new[]
                {
                    ("ข้อใดคือแนวทางปฏิบัติที่ช่วยให้บทความบนหน้าจอมือถือ (Mobile) อ่านง่ายและน่าติดตาม?", new[] { "เขียนย่อหน้ายาว 50 บรรทัดติดกันโดยไม่เว้นบรรทัด", "แบ่งย่อหน้าสั้นๆ ใช้หัวข้อย่อย (Bullet points) และเว้นช่องว่าง (White space) ให้อ่านสบายตา", "ใช้ฟอนต์ตัวเล็กที่สุดเท่าที่เป็นไปได้", "ใส่ตัวอักษรกะพริบทุกจุด" }, 1),
                    ("Copywriting ต่างจากการเขียนบทความทั่วไป (Content Writing) อย่างไรในเชิงเป้าหมาย?", new[] { "Copywriting มุ่งเน้นการโน้มน้าวใจเพื่อให้เกิดการกระทำเฉพาะเจาะจง (เช่น ซื้อสินค้า, สมัครสมาชิก)", "Content Writing ใช้เฉพาะกับการพิมพ์หนังสือพิมพ์", "Copywriting เขียนได้เฉพาะภาษาอังกฤษเท่านั้น", "ไม่มีความแตกต่างกันเลย" }, 0),
                    ("เทคนิค 'FAB' ในการเขียนข้อความเพื่อนำเสนอสินค้า ย่อมาจากคำว่าอะไร?", new[] { "Fast, Accurate, Beautiful", "Features (คุณสมบัติ), Advantages (ข้อได้เปรียบ), Benefits (ประโยชน์ที่ลูกค้าได้รับ)", "Focus, Action, Best", "Free, Available, Bonus" }, 1),
                    ("ขั้นตอนการตรวจทาน (Proofreading) ก่อนเผยแพร่คอนเทนต์ช่วยเรื่องใดมากที่สุด?", new[] { "ตัดคำสะกดผิด แก้ไขไวยากรณ์ และขัดเกลาสำนวนให้กระชับคมคายและน่าเชื่อถือ", "เพิ่มความยาวของบทความ", "เปลี่ยนหัวเรื่องใหม่ทั้งหมด", "ทำให้ไม่ต้องตรวจสอบข้อเท็จจริง" }, 0)
                }
            ),

            // 28. machine-learning-python-basics
            new(
                "machine-learning-python-basics",
                new[]
                {
                    ("การเรียนรู้ของเครื่องแบบ Supervised Learning มีลักษณะเด่นคือข้อใด?", new[] { "ไม่มีข้อมูลผลเฉลย (Labels) ให้ในการฝึก", "มีชุดข้อมูลที่มีทั้งฟีเจอร์นำเข้า (Features) และผลลัพธ์ที่ถูกต้อง (Target/Labels) เพื่อฝึกให้โมเดลเรียนรู้ความสัมพันธ์", "โมเดลเรียนรู้จากการให้รางวัลและลงโทษ", "เป็นการเขียนโปรแกรมตามกฎแบบดั้งเดิม" }, 1),
                    ("ไลบรารียอดนิยมในภาษา Python สำหรับสร้างโมเดล Machine Learning แบบคลาสสิกคือข้อใด?", new[] { "Scikit-Learn", "Flask", "Django", "Pygame" }, 0),
                    ("การแบ่งชุดข้อมูลเป็น Training Set และ Test Set มีวัตถุประสงค์เพื่อสิ่งใด?", new[] { "เพื่อให้ไฟล์ข้อมูลมีขนาดเล็กลง", "เพื่อใช้ข้อมูลชุด Train ในการฝึกโมเดล และใช้ชุด Test ในการประเมินประสิทธิภาพกับข้อมูลใหม่ที่โมเดลไม่เคยเห็น", "เพื่อให้คอมพิวเตอร์ประมวลผลเร็วขึ้น", "เป็นขั้นตอนที่ไม่จำเป็น" }, 1)
                },
                new[]
                {
                    ("ปัญหา 'Overfitting' ใน Machine Learning หมายถึงสิ่งใด?", new[] { "โมเดลเรียนรู้และจดจำรายละเอียดรวมถึงสัญญาณรบกวน (Noise) ในข้อมูล Train มากเกินไป จนทำงานได้แย่มากเมื่อเจอกับข้อมูลใหม่", "โมเดลไม่เรียนรู้อะไรเลยและมีความแม่นยำต่ำมากทั้งบน Train และ Test", "โมเดลประมวลผลได้เร็วเกินไป", "ขนาดหน่วยความจำเต็ม" }, 0),
                    ("ตัวชี้วัด Accuracy (ความถูกต้อง) อาจทำให้เข้าใจผิด (Misleading) ได้ในกรณีข้อมูลแบบใด?", new[] { "ชุดข้อมูลที่มีการกระจายตัวของคลาสไม่สมดุลอย่างมาก (Imbalanced Data เช่น คลาส 0 มี 99% และคลาส 1 มี 1%)", "ชุดข้อมูลที่มีขนาดใหญ่", "ชุดข้อมูลที่เป็นตัวเลขทศนิยม", "ชุดข้อมูลที่ไม่มีค่าว่าง" }, 0),
                    ("โมเดล Logistic Regression นิยมนำมาใช้งานกับปัญหาประเภทใดเป็นหลัก?", new[] { "การทำนายราคาบ้าน (Continuous Regression)", "การจัดหมวดหมู่แบบแบ่งกลุ่มสองทาง (Binary Classification เช่น สแปม หรือ ไม่ใช่สแปม)", "การสร้างภาพสามมิติ", "การแปลภาษาแบบเรียลไทม์" }, 1),
                    ("เทคนิค K-Fold Cross Validation มีประโยชน์หลักในการประเมินโมเดลอย่างไร?", new[] { "ช่วยลดความเอนเอียง (Bias) จากการสุ่มแบ่งข้อมูลเพียงครั้งเดียว โดยหมุนเวียนแบ่งข้อมูลเป็น K ส่วนเพื่อทดสอบให้ครบถ้วน", "เพิ่มจำนวนข้อมูลจริงในฐานข้อมูล", "ทำให้โมเดลไม่ต้องเทรน", "ช่วยลดการใช้แรมของเครื่อง" }, 0)
                }
            ),

            // 29. prompt-engineering-work
            new(
                "prompt-engineering-work",
                new[]
                {
                    ("Prompt Engineering หมายถึงทักษะหรือกระบวนการในเรื่องใด?", new[] { "การซ่อมบำรุงฮาร์ดแวร์เซิร์ฟเวอร์", "ศิลปะและศาสตร์ในการออกแบบข้อความคำสั่งและบริบทอย่างมีระบบ เพื่อให้โมเดล AI สร้างคำตอบที่ตรงเป้าหมายที่สุด", "การเขียนโปรแกรมควบคุมหน้าจอ", "การเขียนเอกสารคู่มือระบบ" }, 1),
                    ("องค์ประกอบสำคัญของ Prompt ที่มีประสิทธิภาพควรมีส่วนประกอบใดบ้าง?", new[] { "Role (บทบาท), Context (บริบท), Task (ภารกิจ), Constraints (ข้อจำกัด), Format (รูปแบบผลลัพธ์)", "ข้อความสั่งสั้นๆ เพียง 1 คำเสมอ", "รหัสผ่านและข้อมูลบัตรเครดิต", "คำศัพท์เทคนิคที่ไม่มีคำแปล" }, 0),
                    ("เทคนิค 'Few-Shot Prompting' มีลักษณะการสั่งงานอย่างไร?", new[] { "สั่งให้ AI ทำงานโดยไม่บอกอะไรเลย", "การใส่ตัวอย่างคำถามและรูปแบบคำตอบที่ถูกต้องประกอบลงใน Prompt (2-3 ตัวอย่าง) เพื่อเป็นแนวทางให้ AI", "การส่ง Prompt ซ้ำๆ 10 ครั้ง", "การยิงคำสั่งผ่านทางโทรศัพท์" }, 1)
                },
                new[]
                {
                    ("เทคนิค 'Chain-of-Thought (CoT)' ช่วยเพิ่มความแม่นยำของ AI ในโจทย์ประเภทใดมากที่สุด?", new[] { "การระบายสีภาพ", "โจทย์การคิดเชิงตรรกะ การคำนวณคณิตศาสตร์ และการตัดสินใจที่มีความซับซ้อนหลายขั้นตอน", "การโหลดรูปภาพจากเว็บ", "การจัดหมวดหมู่อีเมล" }, 1),
                    ("ปรากฏการณ์ 'AI Hallucination' คืออะไร และควรรับมืออย่างไรในการทำงานจริง?", new[] { "AI ตอบคำถามได้เร็วเกินไป", "AI สร้างข้อมูลหรือแต่งข้อเท็จจริงที่ฟังดูน่าเชื่อถือแต่ผิดพลาดขึ้นมา ควรตรวจสอบผลลัพธ์เสมอโดยเฉพาะข้อมูลตัวเลขและการอ้างอิง", "หน้าจอคอมพิวเตอร์ดับกะทันหัน", "การที่ AI ปฏิเสธการตอบคำถาม" }, 1),
                    ("ข้อควรระวังสำคัญที่สุดในการป้อนข้อมูลลงในระบบ Public Generative AI ขององค์กรคือข้อใด?", new[] { "ห้ามพิมพ์เกิน 5 บรรทัด", "ห้ามใส่ข้อมูลส่วนบุคคล (PII), ความลับทางการค้า หรือรหัสผ่านของระบบลงไปโดยเด็ดขาด", "ต้องพิมพ์เป็นตัวพิมพ์ใหญ่ทั้งหมด", "ต้องใช้ภาษาอังกฤษเท่านั้น" }, 1),
                    ("เมื่อผลลัพธ์ของ AI กว้างเกินไปและไม่ตรงกับความต้องการ วิธีการปรับปรุง Prompt ที่ดีที่สุดคือข้อใด?", new[] { "ลบทิ้งและเลิกใช้งาน", "ระบุบริบทเพิ่มเติม กำหนดบทบาทที่ชัดเจน และระบุข้อจำกัดหรือตัวอย่างรูปแบบผลลัพธ์ที่ต้องการ (เช่น สรุปเป็น 3 ข้อสั้นๆ)", "พิมพ์คำสั่งเดิมซ้ำหลายๆ รอบ", "ส่งสติกเกอร์ให้ AI" }, 1)
                }
            )
        };
    }
}
