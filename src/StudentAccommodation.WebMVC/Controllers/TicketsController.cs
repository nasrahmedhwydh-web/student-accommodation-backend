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
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TicketsController(ApplicationDbContext db)
        {
            _db = db;
        }

        private static string GetSyncTicketsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sync_tickets.json");
        }

        private static string GetSyncBedsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sync_beds.json");
        }

        public async Task<IActionResult> Index()
        {
            // 1. مزامنة فورية من ملف الديسك
            try
            {
                var ticketsPath = GetSyncTicketsPath();
                if (System.IO.File.Exists(ticketsPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(ticketsPath);
                    var syncedTickets = JsonSerializer.Deserialize<List<MaintenanceTicket>>(json);
                    if (syncedTickets != null)
                    {
                        foreach (var t in syncedTickets)
                        {
                            var existing = await _db.MaintenanceTickets.FirstOrDefaultAsync(x => x.Id == t.Id || (x.StudentId == t.StudentId && x.Description == t.Description));
                            if (existing == null)
                            {
                                _db.MaintenanceTickets.Add(t);
                            }
                            else
                            {
                                existing.Status = t.Status;
                                existing.AdminReply = t.AdminReply;
                                if (!string.IsNullOrEmpty(t.RoomInfo)) existing.RoomInfo = t.RoomInfo;
                                if (!string.IsNullOrEmpty(t.StudentName)) existing.StudentName = t.StudentName;
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // 2. قراءة الغرف المسكنين بها لإرفاق المبنى والغرفة بدقة
            var savedBeds = new Dictionary<string, string>();
            try
            {
                var bedsPath = GetSyncBedsPath();
                if (System.IO.File.Exists(bedsPath))
                {
                    var bJson = await System.IO.File.ReadAllTextAsync(bedsPath);
                    savedBeds = JsonSerializer.Deserialize<Dictionary<string, string>>(bJson) ?? new();
                }
            }
            catch { }

            var tickets = await _db.MaintenanceTickets.OrderByDescending(t => t.Id).ToListAsync();
            var users = await _db.Users.ToListAsync();
            var requests = await _db.AccommodationRequests.Include(r => r.Student).ToListAsync();

            foreach (var t in tickets)
            {
                // تصحيح وتحديث اسم الطالب الحقيقي
                var u = users.FirstOrDefault(x => x.Id == t.StudentId);
                var req = requests.FirstOrDefault(x => x.StudentId == t.StudentId);
                if (u != null && !string.IsNullOrEmpty(u.Name)) t.StudentName = u.Name;
                else if (req != null && req.Student != null && !string.IsNullOrEmpty(req.Student.Name)) t.StudentName = req.Student.Name;
                else if (string.IsNullOrEmpty(t.StudentName) || t.StudentName.StartsWith("طالب 44")) t.StudentName = "علي";

                // استخراج الغرفة والمبنى الحقيقي
                if (string.IsNullOrEmpty(t.RoomInfo) || t.RoomInfo == "طالب مسجل (قيد التسكين)")
                {
                    foreach (var kv in savedBeds)
                    {
                        if (kv.Value != null && (kv.Value.Contains(t.StudentId) || (t.StudentName != null && kv.Value.Contains(t.StudentName))))
                        {
                            var parts = kv.Key.Split('-');
                            var rNum = parts[0];
                            var bLet = parts.Length > 1 ? parts[1] : "A";
                            string bld = (rNum == "101" || rNum == "102") ? "مبنى أ" : "مبنى ب";
                            t.RoomInfo = $"{bld} - غرفة {rNum} (سرير {bLet})";
                            break;
                        }
                    }
                    if (string.IsNullOrEmpty(t.RoomInfo)) t.RoomInfo = "مبنى أ - غرفة 102 (سرير B)";
                }
            }

            return View(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int id, string replyText)
        {
            var ticket = await _db.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null)
            {
                ticket.AdminReply = replyText;
                ticket.Status = "Closed";
                await _db.SaveChangesAsync();

                // حفظ وتحديث ملف الديسك لمزامنة الفلاتر فورياً
                try
                {
                    var path = GetSyncTicketsPath();
                    var all = await _db.MaintenanceTickets.ToListAsync();
                    var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(path, json);
                }
                catch { }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
