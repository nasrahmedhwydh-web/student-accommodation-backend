using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAccommodation.Domain.Entities;
using StudentAccommodation.Infrastructure.Persistence;

namespace StudentAccommodation.WebMVC.Controllers
{
    public class AllocationsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AllocationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private static string GetSyncRequestsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sync_requests.json");
        }

        private static string GetSyncBedsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sync_beds.json");
        }

        public async Task<IActionResult> Index()
        {
            // 1. مزامنة حالة الطلبات
            try
            {
                var reqPath = GetSyncRequestsPath();
                if (System.IO.File.Exists(reqPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(reqPath);
                    var syncedReqs = JsonSerializer.Deserialize<List<AccommodationRequest>>(json);
                    if (syncedReqs != null)
                    {
                        foreach (var req in syncedReqs)
                        {
                            var existing = await _db.AccommodationRequests.FirstOrDefaultAsync(r => r.StudentId == req.StudentId);
                            if (existing != null)
                            {
                                if (req.FinanceStatus == "Approved") existing.FinanceStatus = "Approved";
                                if (req.AllocationStatus == "Approved") existing.AllocationStatus = "Approved";
                                if (req.Status == "Allocated") existing.Status = "Allocated";
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // 2. مزامنة حالة الأسرة المحفوظة من الديسك
            try
            {
                var bedsPath = GetSyncBedsPath();
                if (System.IO.File.Exists(bedsPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(bedsPath);
                    var savedBeds = JsonSerializer.Deserialize<Dictionary<string, string>>(json); // Key: "101-A", Value: "StudentName|StudentId"
                    if (savedBeds != null)
                    {
                        var allBeds = await _db.Beds.Include(b => b.Room).ToListAsync();
                        foreach (var b in allBeds)
                        {
                            var key = $"{b.Room?.RoomNumber}-{b.BedNumber}";
                            if (savedBeds != null && savedBeds.ContainsKey(key) && !string.IsNullOrEmpty(savedBeds[key]))
                            {
                                b.IsOccupied = true;
                                b.CurrentStudentId = savedBeds[key];
                            }
                            else
                            {
                                b.IsOccupied = false;
                                b.CurrentStudentId = null;
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }

            var beds = await _db.Beds.Include(b => b.Room).ToListAsync();
            var requests = await _db.AccommodationRequests.Include(r => r.Student).ToListAsync();

            ViewBag.Requests = requests;
            return View(beds);
        }

        [HttpPost]
        public async Task<IActionResult> AssignAjax(string roomNumber, string bedLetter, string studentId, string studentName)
        {
            try
            {
                var bed = await _db.Beds.Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Room.RoomNumber == roomNumber && b.BedNumber == bedLetter);

                if (bed != null)
                {
                    bed.IsOccupied = true;
                    bed.CurrentStudentId = $"{studentName} ({studentId})";
                }

                var req = await _db.AccommodationRequests.FirstOrDefaultAsync(r => r.StudentId == studentId || r.Student.Name == studentName);
                if (req != null)
                {
                    req.AllocationStatus = "Approved";
                    req.Status = "Allocated";
                }

                await _db.SaveChangesAsync();

                // حفظ دائم على الديسك لملف الأسرة
                var bedsPath = GetSyncBedsPath();
                var savedBeds = new Dictionary<string, string>();
                if (System.IO.File.Exists(bedsPath))
                {
                    try
                    {
                        var oldJson = await System.IO.File.ReadAllTextAsync(bedsPath);
                        savedBeds = JsonSerializer.Deserialize<Dictionary<string, string>>(oldJson) ?? new();
                    }
                    catch { }
                }
                savedBeds[$"{roomNumber}-{bedLetter}"] = $"{studentName} ({studentId})";
                await System.IO.File.WriteAllTextAsync(bedsPath, JsonSerializer.Serialize(savedBeds, new JsonSerializerOptions { WriteIndented = true }));

                // حفظ دائم على الديسك لملف الطلبات
                var reqPath = GetSyncRequestsPath();
                var allReqs = await _db.AccommodationRequests.Include(r => r.Student).ToListAsync();
                await System.IO.File.WriteAllTextAsync(reqPath, JsonSerializer.Serialize(allReqs, new JsonSerializerOptions { WriteIndented = true }));

                return Json(new { success = true, message = "تم التسكين وحفظه دائماً بنجاح" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UnassignAjax(string roomNumber, string bedLetter)
        {
            try
            {
                var bed = await _db.Beds.Include(b => b.Room)
                    .FirstOrDefaultAsync(b => b.Room.RoomNumber == roomNumber && b.BedNumber == bedLetter);

                string freedStudent = "";
                if (bed != null)
                {
                    freedStudent = bed.CurrentStudentId ?? "";
                    bed.IsOccupied = false;
                    bed.CurrentStudentId = null;
                }

                // إزالة من ملف الأسرة على الديسك
                var bedsPath = GetSyncBedsPath();
                if (System.IO.File.Exists(bedsPath))
                {
                    var oldJson = await System.IO.File.ReadAllTextAsync(bedsPath);
                    var savedBeds = JsonSerializer.Deserialize<Dictionary<string, string>>(oldJson) ?? new();
                    if (savedBeds.ContainsKey($"{roomNumber}-{bedLetter}"))
                    {
                        if (string.IsNullOrEmpty(freedStudent))
                        {
                            freedStudent = savedBeds[$"{roomNumber}-{bedLetter}"];
                        }
                        savedBeds.Remove($"{roomNumber}-{bedLetter}");
                        await System.IO.File.WriteAllTextAsync(bedsPath, JsonSerializer.Serialize(savedBeds, new JsonSerializerOptions { WriteIndented = true }));
                    }
                }

                // تحديث حالة الطلب ليعود جاهزاً للتسكين
                var allReqs = await _db.AccommodationRequests.Include(r => r.Student).ToListAsync();
                foreach (var req in allReqs)
                {
                    if (!string.IsNullOrEmpty(freedStudent) &&
                        (freedStudent.Contains(req.StudentId) || (req.Student != null && freedStudent.Contains(req.Student.Name))))
                    {
                        req.FinanceStatus = "Approved";
                        req.AllocationStatus = "ReadyForBed";
                        req.Status = "ReadyForBed";
                    }
                }

                await _db.SaveChangesAsync();

                // تحديث ملف الطلبات على الديسك
                var reqPath = GetSyncRequestsPath();
                await System.IO.File.WriteAllTextAsync(reqPath, JsonSerializer.Serialize(allReqs, new JsonSerializerOptions { WriteIndented = true }));

                return Json(new { success = true, message = "تم إخلاء السرير وعودة الطالب لقائمة الجاهزون للتسكين" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}
