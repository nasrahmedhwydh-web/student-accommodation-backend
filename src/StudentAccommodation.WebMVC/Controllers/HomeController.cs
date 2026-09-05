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
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
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
            // 1. مزامنة الطلبات اللحظية من الديسك
            try
            {
                var syncPath = GetSyncRequestsPath();
                if (System.IO.File.Exists(syncPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(syncPath);
                    var syncedReqs = JsonSerializer.Deserialize<List<AccommodationRequest>>(json);
                    if (syncedReqs != null && syncedReqs.Count > 0)
                    {
                        foreach (var req in syncedReqs)
                        {
                            var existing = await _db.AccommodationRequests.FirstOrDefaultAsync(r => r.StudentId == req.StudentId);
                            if (existing == null)
                            {
                                if (req.Student != null && !await _db.Users.AnyAsync(u => u.Id == req.StudentId))
                                {
                                    _db.Users.Add(req.Student);
                                }
                                _db.AccommodationRequests.Add(new AccommodationRequest
                                {
                                    StudentId = req.StudentId,
                                    AcademicStatus = req.AcademicStatus,
                                    SecurityStatus = req.SecurityStatus,
                                    FinanceStatus = req.FinanceStatus,
                                    BankReceiptPath = req.BankReceiptPath,
                                    Status = req.Status,
                                    CreatedAt = req.CreatedAt
                                });
                            }
                            else
                            {
                                if (existing.FinanceStatus != "Approved")
                                {
                                    existing.FinanceStatus = req.FinanceStatus;
                                }
                                existing.AcademicStatus = req.AcademicStatus;
                                existing.SecurityStatus = req.SecurityStatus;
                                if (!string.IsNullOrEmpty(req.BankReceiptPath)) existing.BankReceiptPath = req.BankReceiptPath;
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // 2. مزامنة الأسرة والتسكين المحفوظ من الديسك
            try
            {
                var bedsPath = GetSyncBedsPath();
                if (System.IO.File.Exists(bedsPath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(bedsPath);
                    var savedBeds = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (savedBeds != null)
                    {
                        var allBeds = await _db.Beds.Include(b => b.Room).ToListAsync();
                        foreach (var b in allBeds)
                        {
                            var key = $"{b.Room?.RoomNumber}-{b.BedNumber}";
                            if (savedBeds.ContainsKey(key) && !string.IsNullOrEmpty(savedBeds[key]))
                            {
                                b.IsOccupied = true;
                                b.CurrentStudentId = savedBeds[key];
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // 3. مزامنة البلاغات والدعم الفني المحفوظة من الديسك
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
                var ticketsPath = Path.Combine(dir, "sync_tickets.json");
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
                            }
                        }
                        await _db.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // استرجاع لحظي وفوري وحقيقي 100% من قاعدة البيانات
            var requests = await _db.AccommodationRequests.Include(r => r.Student).OrderByDescending(r => r.Id).ToListAsync();
            var beds = await _db.Beds.Include(b => b.Room).ToListAsync();
            var tickets = await _db.MaintenanceTickets.ToListAsync();

            // حساب الإحصائيات الحقيقية تماماً
            int totalBeds = beds.Count;
            int occupiedBeds = beds.Count(b => b.IsOccupied);
            int vacantBeds = beds.Count(b => !b.IsOccupied);
            int occupancyRate = totalBeds > 0 ? (int)Math.Round((double)occupiedBeds / totalBeds * 100) : 0;

            // حساب نسبة الإشغال لكل مبنى
            var buildingABeds = beds.Where(b => b.Room != null && (b.Room.RoomNumber == "101" || b.Room.RoomNumber == "102")).ToList();
            var buildingBBeds = beds.Where(b => b.Room != null && (b.Room.RoomNumber == "103" || b.Room.RoomNumber == "104")).ToList();

            int buildingAOccupied = buildingABeds.Count(b => b.IsOccupied);
            int buildingBOccupied = buildingBBeds.Count(b => b.IsOccupied);

            int buildingARate = buildingABeds.Count > 0 ? (int)Math.Round((double)buildingAOccupied / buildingABeds.Count * 100) : 0;
            int buildingBRate = buildingBBeds.Count > 0 ? (int)Math.Round((double)buildingBOccupied / buildingBBeds.Count * 100) : 0;

            int pendingRequests = requests.Count(r => r.FinanceStatus != "Approved");
            int openTickets = tickets.Count(t => t.Status == "Open");

            ViewBag.OccupancyRate = occupancyRate;
            ViewBag.BuildingARate = buildingARate;
            ViewBag.BuildingBRate = buildingBRate;
            ViewBag.PendingRequestsCount = pendingRequests;
            ViewBag.VacantBedsCount = vacantBeds;
            ViewBag.OpenTicketsCount = openTickets;
            ViewBag.TotalStudents = occupiedBeds;

            return View(requests);
        }
    }
}
