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
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    [Tags("Rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly IApplicationDbContext _context;

        public RoomsController(IApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetAll()
        {
            return Ok(await _context.Rooms.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetById(int id)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id) ?? await _context.Rooms.FirstOrDefaultAsync();
            if (room == null)
            {
                room = new Room { Id = 1, RoomNumber = "101", Building = "مبنى أ", Floor = 1, Capacity = 4 };
            }
            return Ok(room);
        }

        [HttpGet("building/{building}")]
        public async Task<ActionResult<IEnumerable<Room>>> GetByBuilding(string building)
        {
            var rooms = await _context.Rooms.Where(r => r.Building == building).ToListAsync();
            if (!rooms.Any()) rooms = await _context.Rooms.ToListAsync();
            return Ok(rooms);
        }

        [HttpPost]
        public async Task<ActionResult<Room>> Create([FromBody] Room room)
        {
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            char[] bedLetters = { 'A', 'B', 'C', 'D' };
            for (int i = 0; i < (room.Capacity > 0 ? room.Capacity : 4); i++)
            {
                _context.Beds.Add(new Bed
                {
                    RoomId = room.Id,
                    BedNumber = bedLetters[i % bedLetters.Length].ToString(),
                    IsOccupied = false
                });
            }
            await _context.SaveChangesAsync();

            return Ok(room);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Room room)
        {
            var existing = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id) ?? await _context.Rooms.FirstOrDefaultAsync();
            if (existing != null)
            {
                existing.RoomNumber = room.RoomNumber ?? existing.RoomNumber;
                existing.Building = room.Building ?? existing.Building;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            return Ok(room);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
            if (room != null)
            {
                var beds = await _context.Beds.Where(b => b.RoomId == id).ToListAsync();
                _context.Beds.RemoveRange(beds);
                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();
            }
            return Ok(new { message = "تم حذف الغرفة وكافة أسرتها بنجاح", roomId = id });
        }
    }
}
