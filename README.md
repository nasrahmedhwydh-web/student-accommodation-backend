# 🏢 نظام إدارة وتسكين الطلاب الذكي - Backend (.NET 8.0)
### Smart Student Accommodation Management System (WebAPI + WebMVC)

نظام متكامل ومتقدم لإدارة وتسكين الطلاب الجامعيين مبني باستخدام أحدث تقنيات **ASP.NET Core 8.0** بنمط المعمارية النظيفة (**Clean Architecture**). يجمع هذا المستودع كلاً من:
1. 🌐 **خادم الـ WebAPI:** لخدمة وتغذية تطبيق الجوال (Flutter) والتطبيقات الخارجية بتقنية RESTful API موثقة بـ Swagger.
2. 💻 **لوحة تحكم المشرف والإدارة (WebMVC):** لوحة تحكم عصرية تفاعلية بنظام السحب والإفلات (Drag & Drop) لإدارة توزيع الأسرة، تدقيق الطلبات والسندات المالية، وإدارة تذاكر الدعم والصيانة.

---

## 🏛️ معمارية النظام (Clean Architecture Structure)

تم تنظيم المشروع في طبقات مستقلة ومنفصلة لضمان قابلية التوسع والصيانة العالية:

```
src/
├── StudentAccommodation.Domain/          # الكيانات الأساسية وقواعد العمل (Entities)
│   ├── Entities/ (Student, User, Room, Bed, AccommodationRequest, MaintenanceTicket)
│
├── StudentAccommodation.Application/     # واجهات الخدمات والنماذج الوسيطة (Interfaces & DTOs)
│   ├── Interfaces/ (IApplicationDbContext)
│   └── DTOs/ (Auth, Request, Bed, Ticket)
│
├── StudentAccommodation.Infrastructure/  # طبقة البيانات والاتصال بقاعدة البيانات (Persistence)
│   ├── Persistence/ (ApplicationDbContext)
│
├── StudentAccommodation.WebAPI/          # خادم الـ RESTful API
│   ├── Controllers/ (AccommodationRequests, Auth, Beds, Rooms, Users, MaintenanceTickets)
│
└── StudentAccommodation.WebMVC/         # لوحة تحكم وإدارة الإسكان الجامعي
    ├── Controllers/ (Allocations, Requests, Tickets, Home)
    └── Views/ (واجهات السحب والإفلات، تدقيق الطلبات، المحادثات)
```

---

## ✨ المميزات الرئيسية (Core Features)

### 1️⃣ لوحة تحكم المشرف (WebMVC):
* 🎯 **توزيع وتسكين الأسرة الذكي (Interactive Bed Allocation):**
  * سحب وإفلات الطلاب (Drag & Drop) مباشرة على الأسرة الشاغرة (A, B, C, D) للغرف في المباني.
  * إخلاء السرير بضغطة زر مع عودة الطالب التلقائية لقائمة "جاهزون للتسكين".
* 📑 **تدقيق واعتماد الطلبات الواردة (Requests Review):**
  * تدقيق السند المالي (1500 ريال) بضغطة واحدة (`مقبول مالياً` / `بانتظار المطابقة`).
  * تتبع الحالة الأكاديمية والموافقة الأمنية.
* 💬 **نظام التذاكر والدعم الفني (Support & Ticketing):**
  * استقبال بلاغات الطلاب والرد المباشر عليها مع عزل كامل لكل محادثة طالب.

### 2️⃣ واجهات الـ WebAPI (RESTful Endpoints):
* 🔐 **`POST /api/Auth/login` & `register`:** توثيق الطلاب وإرجاع JWT Token.
* 📄 **`POST /api/AccommodationRequests/submit-student-docs`:** استلام الوثائق الأربعة وسند السداد.
* 📊 **`GET /api/AccommodationRequests/student/{studentId}`:** استرجاع حالة الطالب اللحظية ورقم غرفته وسريره.
* 🛏️ **`GET /api/Beds` & `POST /api/Beds/allocate` & `unassign`:** إدارة الأسرة.
* 💬 **`GET /api/MaintenanceTickets/student/{studentId}` & `POST`:** إدارة المحادثات والدعم.

---

## 🚀 كيفية تشغيل المشروع (How to Run)

### 1. المتطلبات:
* مثبت حزمة **.NET SDK 8.0** أو أحدث.
* بيئة التطوير **Visual Studio 2022** أو **VS Code**.

### 2. تشغيل الـ WebAPI (الخادم):
```bash
cd src/StudentAccommodation.WebAPI
dotnet run
```
* 🌐 رابط التوثيق التفاعلي Swagger: `http://localhost:62129/index.html`

### 3. تشغيل لوحة المشرفين (WebMVC):
```bash
cd src/StudentAccommodation.WebMVC
dotnet run
```
* 🖥️ رابط لوحة الإدارة: `http://localhost:62128`

---

## 👨‍💻 تم الإعداد والتطوير بواسطة:
* **إعداد وتطوير المهندس:** نصر أحمد ناجي هويده 🎓
* **تحت إشراف الدكتور:** د. إبراهيم البلطة
* **الجامعة:** جامعة الحكمة - كلية العلوم والهندسة
* **المشروع:** مشروع تدريب ميداني / نظام إدارة وتسكين الطلاب الذكي (Backend - WebAPI + WebMVC)
