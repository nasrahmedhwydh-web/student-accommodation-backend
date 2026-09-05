using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAccommodation.Application.Interfaces;
using StudentAccommodation.Domain.Entities;

namespace StudentAccommodation.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [Tags("MaintenanceTickets")]
    public class MaintenanceTicketsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public MaintenanceTicketsController(IApplicationDbContext context)
        {
            _context = context;
        }

        private static string GetSyncTicketsPath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "sync_tickets.json");
        }

        private void SyncTicketsToDisk()
        {
            try
            {
                var list = _context.MaintenanceTickets.ToList();
                var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(GetSyncTicketsPath(), json);
            }
            catch { }
        }

        private async Task ReadSyncFromDiskAsync()
        {
            try
            {
                var path = GetSyncTicketsPath();
                if (System.IO.File.Exists(path))
                {
                    var json = await System.IO.File.ReadAllTextAsync(path);
                    var synced = JsonSerializer.Deserialize<List<MaintenanceTicket>>(json);
                    if (synced != null)
                    {
                        foreach (var s in synced)
                        {
                            var existing = await _context.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == s.Id);
                            if (existing != null)
                            {
                                existing.AdminReply = s.AdminReply;
                                existing.Status = s.Status;
                                existing.StudentName = s.StudentName;
                                existing.RoomInfo = s.RoomInfo;
                            }
                            else
                            {
                                _context.MaintenanceTickets.Add(s);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceTicket>>> GetAll()
        {
            await ReadSyncFromDiskAsync();
            var list = await _context.MaintenanceTickets.OrderByDescending(t => t.Id).ToListAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceTicket>> GetById(int id)
        {
            await ReadSyncFromDiskAsync();
            var ticket = await _context.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id) ?? await _context.MaintenanceTickets.FirstOrDefaultAsync();
            return Ok(ticket);
        }

        [HttpGet("student/{studentId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceTicket>>> GetByStudent(string studentId)
        {
            await ReadSyncFromDiskAsync();
            var cleanId = studentId?.Trim('{', '}', ' ', '"') ?? string.Empty;
            var list = await _context.MaintenanceTickets
                .Where(t => t.StudentId.ToLower() == cleanId.ToLower() || (t.StudentName != null && t.StudentName.ToLower().Contains(cleanId.ToLower())))
                .OrderByDescending(t => t.Id)
                .ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<ActionResult<MaintenanceTicket>> Create([FromBody] MaintenanceTicket ticket)
        {
            var u = await _context.Users.FirstOrDefaultAsync(user => user.Id == ticket.StudentId);
            var req = await _context.AccommodationRequests.Include(r => r.Student).FirstOrDefaultAsync(r => r.StudentId == ticket.StudentId);
            
            if (u != null && !string.IsNullOrEmpty(u.Name)) ticket.StudentName = u.Name;
            else if (req != null && req.Student != null && !string.IsNullOrEmpty(req.Student.Name)) ticket.StudentName = req.Student.Name;
            else if (string.IsNullOrEmpty(ticket.StudentName)) ticket.StudentName = $"طالب {ticket.StudentId}";

            string roomInfo = "طالب مسجل (قيد التسكين)";
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudentAccommodation");
                var bedsPath = Path.Combine(dir, "sync_beds.json");
                if (System.IO.File.Exists(bedsPath))
                {
                    var bedsJson = await System.IO.File.ReadAllTextAsync(bedsPath);
                    var savedBeds = JsonSerializer.Deserialize<Dictionary<string, string>>(bedsJson);
                    if (savedBeds != null)
                    {
                        foreach (var kv in savedBeds)
                        {
                            if (kv.Value != null && (kv.Value.Contains(ticket.StudentId) || (ticket.StudentName != null && kv.Value.Contains(ticket.StudentName))))
                            {
                                var parts = kv.Key.Split('-');
                                var rNum = parts[0];
                                var bLet = parts.Length > 1 ? parts[1] : "A";
                                string bld = (rNum == "101" || rNum == "102") ? "مبنى أ" : "مبنى ب";
                                roomInfo = $"{bld} - غرفة {rNum} (سرير {bLet})";
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            ticket.RoomInfo = roomInfo;
            ticket.CreatedAt = DateTime.UtcNow;
            _context.MaintenanceTickets.Add(ticket);
            await _context.SaveChangesAsync();
            SyncTicketsToDisk();
            return Ok(ticket);
        }

        [HttpPost("reply/{id}")]
        [HttpPut("reply/{id}")]
        public async Task<IActionResult> Reply(int id, [FromBody] JsonElement body)
        {
            var ticket = await _context.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null)
            {
                if (body.TryGetProperty("reply", out var r)) ticket.AdminReply = r.GetString();
                ticket.Status = "Closed";
                await _context.SaveChangesAsync();
                SyncTicketsToDisk();
                return Ok(new { success = true, message = "تم إرسال رد المشرف وحفظه بنجاح" });
            }
            return NotFound(new { error = "التذكرة غير موجودة" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.MaintenanceTickets.FirstOrDefaultAsync(t => t.Id == id);
            if (ticket != null)
            {
                _context.MaintenanceTickets.Remove(ticket);
                await _context.SaveChangesAsync();
                SyncTicketsToDisk();
            }
            return Ok(new { message = "تم حذف التذكرة بنجاح", id = id });
        }
    }
}
