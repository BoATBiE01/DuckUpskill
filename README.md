# 🦆 DuckUpskill

แพลตฟอร์มคอร์สเรียนออนไลน์สำหรับอัปสกิล พัฒนาด้วย ASP.NET Core MVC และ SQL Server

## ฟีเจอร์หลัก

- คอร์สฟรีและคอร์สเสียเงิน พร้อม Mock Checkout
- สมัครเรียน ดูบทเรียน และติดตามความคืบหน้า
- Pre-test / Post-test แบบปรนัย 4 ตัวเลือก
- Post-test และ Certificate หลังเรียนจบ
- หน้า Profile พร้อมอัปโหลดรูปและลบบัญชีสมาชิก
- Admin Dashboard
- CRUD คอร์ส บทเรียน สมาชิก คำสั่งซื้อ และข้อสอบ
- หน้า Support สำหรับช่องทางติดต่อ LINE, Facebook, Instagram และอีเมล

## Tech Stack

- ASP.NET Core MVC / .NET 10
- Entity Framework Core
- SQL Server / Azure SQL Database
- ASP.NET Core Identity
- Bootstrap 5 และ CSS ธีมเหลือง–ส้ม

## รันในเครื่อง

```powershell
cd C:\Users\boatk\source\repos\DuckUpskill
dotnet restore
dotnet run
```

ค่าเริ่มต้นในเครื่องใช้ SQL Server Express:

```text
Server=localhost\SQLEXPRESS;Database=DuckUpskillDb;Trusted_Connection=True;TrustServerCertificate=True
```

เปิดหน้าเว็บจาก URL ที่แสดงใน Terminal หรือ Visual Studio

## บัญชี Demo สำหรับเครื่องทดสอบ

> เปลี่ยนรหัสผ่านก่อน Deploy จริง และห้ามใช้รหัสผ่าน Demo ใน Production

| Role | Email | Password |
|---|---|---|
| Admin | `admin@duckupskill.com` | `Admin@Duck123!` |
| Member | `student@duckupskill.com` | `Student@Duck123!` |

## เตรียม Deploy บน Azure

โปรเจกต์นี้เตรียมไว้สำหรับ:

- **Azure App Service** สำหรับรันเว็บ ASP.NET Core
- **Azure SQL Database** สำหรับฐานข้อมูล SQL Server
- **GitHub Actions** สำหรับ Build และ Deploy อัตโนมัติ

### 1. สร้าง Azure SQL Database

สร้าง Azure SQL Database และคัดลอก Connection String ของ ADO.NET / SQL Authentication หรือ Managed Identity ตามวิธีที่เลือก

### 2. ตั้งค่า App Service Environment Variables

ใน Azure Portal ไปที่:

```text
App Service → Settings → Environment variables → App settings
```

เพิ่มค่า:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<Azure SQL connection string>
```

ใช้เครื่องหมาย `__` ในชื่อ Setting เพื่อให้ ASP.NET Core อ่านเป็น `ConnectionStrings:DefaultConnection`

> อย่าใส่ Connection String จริงลงใน `appsettings.json` หรือ GitHub

### 3. ตั้งค่า GitHub Actions

ไฟล์ Workflow อยู่ที่:

```text
.github/workflows/azure-webapp.yml
```

แก้ค่าในไฟล์นี้:

```yaml
env:
  AZURE_WEBAPP_NAME: ชื่อ-App-Service-ของคุณ
```

จากนั้นใน Azure App Service เลือก **Get publish profile** แล้วนำค่าที่ได้ไปเพิ่มใน GitHub:

```text
Repository → Settings → Secrets and variables → Actions → New repository secret
Name: AZUREAPPSERVICE_PUBLISHPROFILE
Value: เนื้อหา publish profile XML
```

เมื่อ Push ไปที่ branch `main` GitHub Actions จะ Restore, Build, Publish และ Deploy ให้อัตโนมัติ

### 4. ตรวจสอบหลัง Deploy

ตรวจสอบหน้าเหล่านี้:

```text
/
/Courses
/Account/Login
/Account/AdminLogin
/Support
```

ตรวจ App Service Log stream หากเว็บเปิดไม่ได้:

```text
Azure Portal → App Service → Monitoring → Log stream
```

## ข้อควรระวังสำหรับ Production

- บัญชี Demo ใน `Data/DbInitializer.cs` เป็นข้อมูลสำหรับการเรียน/ทดสอบเท่านั้น
- ควรเปลี่ยนกลไกสร้าง Admin ให้ใช้ Secret หรือสร้างบัญชีครั้งแรกแบบปลอดภัยก่อนเปิดใช้งานจริง
- Mock Checkout ยังไม่ใช่การรับเงินจริง
- เปลี่ยน URL ช่องทาง Social Media ใน `Views/Home/Support.cshtml` เป็น URL จริง
- สำรอง Azure SQL ก่อนแก้ Schema หรือข้อมูลจำนวนมาก
- `wwwroot/uploads` ถูกตั้งเป็นไฟล์ local; หากรันหลาย instance ควรย้ายรูปไป Azure Blob Storage
