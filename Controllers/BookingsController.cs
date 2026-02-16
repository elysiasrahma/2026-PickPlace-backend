using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Models;

namespace PickPlace.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Bookings
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Booking>>> GetBookings(string? search, string? status, bool? isHistory)
        {
            var query = _context.Bookings.Include(b => b.Room).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b =>
                    (b.BorrowerName != null && b.BorrowerName.Contains(search)) ||
                    (b.Organization != null && b.Organization.Contains(search)) ||
                    (b.Purpose != null && b.Purpose.Contains(search)));
            }

            if (isHistory ?? false)
            {
                var threeMonthsAgo = DateTime.Now.AddMonths(-3);
                query = query.Where(b => b.EndTime < DateTime.Now && b.EndTime >= threeMonthsAgo);
            }
            else
            {
                query = query.Where(b => b.EndTime >= DateTime.Now);
            }

            var bookingsList = await query.OrderByDescending(b => b.StartTime).ToListAsync();

            foreach (var b in bookingsList)
            {
                if (b.Status == "Approved")
                {
                    if (DateTime.Now > b.EndTime)
                    {
                        b.Status = "Completed";
                    }
                    else if (DateTime.Now >= b.StartTime && DateTime.Now <= b.EndTime)
                    {
                        b.Status = "On Going";
                    }
                }
            }

            if (!string.IsNullOrEmpty(status))
            {
                bookingsList = bookingsList
                    .Where(b => b.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return Ok(bookingsList);
        }
        // GET: api/Bookings/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Booking>> GetBooking(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Room).FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return booking;
        }

        // PUT: api/Bookings/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBooking(int id, Booking booking)
        {
            if (id != booking.Id)
            {
                return BadRequest();
            }

            _context.Entry(booking).State = EntityState.Modified;

            _context.Entry(booking).Property(x => x.Status).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id))
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

        // POST: api/Bookings
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Booking>> PostBooking(Booking booking)
        {
            var roomExists = await _context.Rooms.AnyAsync(r => r.Id == booking.RoomId);
            if (!roomExists)
            {
                return BadRequest($"Ruangan dengan ID {booking.RoomId} tidak ditemukan!");
            }

            if (booking.EndTime <= booking.StartTime)
            {
                return BadRequest("Jam selesai tidak boleh lebih awal dari jam mulai!");
            }

            booking.Status = "Pending";

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBooking", new { id = booking.Id }, booking);
        }

        // DELETE: api/Bookings/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(e => e.Id == id);
        }

        // PUT: api/Bookings/5/status
        // Endpoint khusus untuk menyetujui/menolak peminjaman
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            // Validasi Pilihan Status
            string[] allowedStatus = { "Approved", "Rejected", "Completed", "Canceled", "Pending" };

            if (newStatus == "Canceled" && booking.StartTime < DateTime.Now)
            {
                return BadRequest("Tidak bisa membatalkan acara yang sudah lewat atau sedang berjalan!");
            }

            if (!allowedStatus.Contains(newStatus))
            {
                return BadRequest("Status tidak valid. Pilih: Approved, Rejected, Completed, Canceled");
            }

            booking.Status = newStatus;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Status berhasil diubah menjadi {newStatus}" });
        }
    }

}
