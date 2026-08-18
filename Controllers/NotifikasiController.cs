using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Helpers;
using PickPlace.Api.Models;

namespace PickPlace.Api.Controllers
{
    [Route("api/notifikasi")]
    [ApiController]
    [Authorize]
    public class NotifikasiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotifikasiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── GET /api/notifikasi?page=1&pageSize=20 ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? onlyUnread = null)
        {
            var userId = JwtHelper.GetUserId(User);

            var query = _context.Notifikasi
                .Where(n => n.UserId == userId)
                .AsQueryable();

            if (onlyUnread == true)
                query = query.Where(n => !n.IsRead);

            query = query.OrderByDescending(n => n.CreatedAt);

            var result = await query.ToPagedResultAsync(page, pageSize);
            return Ok(result);
        }

        // ─── PUT /api/notifikasi/{id}/read ───────────────────────────────────────
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = JwtHelper.GetUserId(User);
            var notif  = await _context.Notifikasi
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { pesan = "Notifikasi ditandai sudah dibaca." });
        }

        // ─── PUT /api/notifikasi/read-all ────────────────────────────────────────
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = JwtHelper.GetUserId(User);
            var unread = await _context.Notifikasi
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { pesan = $"{unread.Count} notifikasi ditandai sudah dibaca." });
        }
    }
}
