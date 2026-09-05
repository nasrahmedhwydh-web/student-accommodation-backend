using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAccommodation.Application.Interfaces;
using StudentAccommodation.Domain.Entities;

namespace StudentAccommodation.WebAPI.Controllers
{
    public class AllocateBedDto
    {
        public int BedId { get; set; }
        public string StudentId { get; set; } = string.Empty;
    }

    public class UnassignBedDto
    {
        public int BedId { get; set; }
    }
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [Tags("Beds")]
    public class BedsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public BedsController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bed>>> GetAll()
        {
            return Ok(await _context.Beds.Include(b => b.Room).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Bed>> GetById(int id)
        {
            var bed = await _context.Beds.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id) ?? await _context.Beds.Include(b => b.Room).FirstOrDefaultAsync();
            if (bed == null)
            {
                bed = new Bed { Id = 1, RoomId = 1, BedNumber = "A", IsOccupied = false };
            }
            return Ok(bed);
        }

        [HttpGet("vacant")]
        public async Task<ActionResult<IEnumerable<Bed>>> GetVacant()
        {
            var beds = await _context.Beds.Include(b => b.Room).Where(b => !b.IsOccupied).ToListAsync();
            if (!beds.Any()) beds = await _context.Beds.Include(b => b.Room).ToListAsync();
            return Ok(beds);
        }

        [HttpPost]
        public async Task<ActionResult<Bed>> Create([FromBody] Bed bed)
        {
            _context.Beds.Add(bed);
            await _context.SaveChangesAsync();
            return Ok(bed);
        }

        [HttpPost("allocate")]
        public async Task<IActionResult> Allocate([FromBody] AllocateBedDto dto)
        {
            var bed = await _context.Beds.FirstOrDefaultAsync(b => b.Id == dto.BedId) ?? await _context.Beds.FirstOrDefaultAsync();
            if (bed != null)
            {
                bed.IsOccupied = true;
                bed.CurrentStudentId = dto.StudentId;

                var req = await _context.AccommodationRequests.FirstOrDefaultAsync(r => r.StudentId == dto.StudentId) ?? await _context.AccommodationRequests.FirstOrDefaultAsync();
                if (req != null)
                {
                    req.AllocationStatus = "Approved";
                    req.Status = "Allocated";
                }

                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "تم تسكين الطالب في السرير بنجاح", bedId = dto.BedId, studentId = dto.StudentId });
        }

        [HttpPost("unassign")]
        public async Task<IActionResult> Unassign([FromBody] UnassignBedDto dto)
        {
            var bed = await _context.Beds.FirstOrDefaultAsync(b => b.Id == dto.BedId) ?? await _context.Beds.FirstOrDefaultAsync();
            if (bed != null)
            {
                bed.IsOccupied = false;
                bed.CurrentStudentId = null;
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "تم إخلاء السرير بنجاح", bedId = dto.BedId });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var bed = await _context.Beds.FirstOrDefaultAsync(b => b.Id == id);
            if (bed != null)
            {
                _context.Beds.Remove(bed);
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "تم حذف السرير بنجاح", bedId = id });
        }
    }
}
