using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAccommodation.Domain.Entities;
using StudentAccommodation.Infrastructure.Persistence;

namespace StudentAccommodation.WebMVC.Controllers
{
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpClientFactory _clientFactory;

        public RequestsController(ApplicationDbContext db, IHttpClientFactory clientFactory)
        {
            _db = db;
            _clientFactory = clientFactory;
        }

        private static string GetSyncPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sync_requests.json");
        }

        public async Task<IActionResult> Index(string? search, string? filter)
        {
            // 1. قراءة ومزامنة الطلبات الحقيقية
            try
            {
                var syncPath = GetSyncPath();
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
                                // الاحتفاظ بالاعتماد المالي المعتمد دائماً
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

            // 2. الاستعلام السريع وتطبيق الفلاتر والبحث
            var query = _db.AccommodationRequests.Include(r => r.Student).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => 
                    r.StudentId.Contains(search) || 
                    (r.Student != null && r.Student.Name.Contains(search))
                );
            }

            if (filter == "academic") query = query.Where(r => r.AcademicStatus == "Approved");
            else if (filter == "security") query = query.Where(r => r.SecurityStatus == "Approved");
            else if (filter == "receipt") query = query.Where(r => !string.IsNullOrEmpty(r.BankReceiptPath) && r.FinanceStatus == "Pending");

            var requests = await query.OrderByDescending(r => r.Id).ToListAsync();
            var allRequests = await _db.AccommodationRequests.ToListAsync();

            ViewBag.TotalCount = allRequests.Count;
            ViewBag.ApprovedAcademic = allRequests.Count(r => r.AcademicStatus == "Approved");
            ViewBag.ApprovedSecurity = allRequests.Count(r => r.SecurityStatus == "Approved");
            ViewBag.PendingReceipt = allRequests.Count(r => !string.IsNullOrEmpty(r.BankReceiptPath) && r.FinanceStatus == "Pending");

            return View(requests);
        }

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Verify(string studentId, string stage, string status)
        {
            var req = await _db.AccommodationRequests.FirstOrDefaultAsync(r => r.StudentId == studentId);
            if (req != null)
            {
                if (stage == "academic") req.AcademicStatus = status;
                else if (stage == "security") req.SecurityStatus = status;
                else if (stage == "finance")
                {
                    req.FinanceStatus = status;
                    req.Status = "ReadyForBed";
                    req.AllocationStatus = "ReadyForBed";
                }
                await _db.SaveChangesAsync();

                // مزامنة فورية في ملف الديسك والـ API
                try
                {
                    var syncPath = GetSyncPath();
                    var all = await _db.AccommodationRequests.Include(r => r.Student).ToListAsync();
                    var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
                    await System.IO.File.WriteAllTextAsync(syncPath, json);
                }
                catch { }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
