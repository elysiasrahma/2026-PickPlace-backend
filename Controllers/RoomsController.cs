using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Models;

namespace PickPlace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            return await _context.Rooms
                .Where(r => !r.IsDeleted) 
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            return room;
        }

        [HttpPost]
        public async Task<ActionResult<Room>> PostRoom(Room room)
        {
            bool nameExists = await _context.Rooms.AnyAsync(r => r.RoomName == room.RoomName);
            if (nameExists)
            {
                return BadRequest($"Nama ruangan '{room.RoomName}' sudah ada! Gunakan nama lain.");
            }
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, room);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoom(int id, Room room)
        {
            if (id != room.Id)
            {
                return BadRequest();
            }

            bool nameExists = await _context.Rooms
                .AnyAsync(r => r.RoomName == room.RoomName && r.Id != id);
                
            if (nameExists)
            {
                return BadRequest($"Nama ruangan '{room.RoomName}' sudah digunakan ruangan lain.");
            }

            _context.Entry(room).State = EntityState.Modified;

            _context.Entry(room).Property(x => x.IsDeleted).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Rooms.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();
            room.IsDeleted = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}