using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Helpers;
using PickPlace.Api.Models;

namespace PickPlace.Api.Controllers
{
    [Route("api/organisasi")]
    [ApiController]
    [Authorize(Roles = "Organisasi")]
    public class OrganisasiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OrganisasiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: ambil organisasi milik user yang sedang login
        private async Task<Organisasi?> GetMyOrg()
        {
            var userId = JwtHelper.GetUserId(User);
            return await _context.Organisasi.FirstOrDefaultAsync(o => o.UserId == userId);
        }

        // ─── GET /api/organisasi/anggota ─────────────────────────────────────────
        [HttpGet("anggota")]
        public async Task<IActionResult> GetAnggota()
        {
            var org = await GetMyOrg();
            if (org == null) return Forbid();

            var anggota = await _context.AnggotaOrganisasi
                .Where(a => a.OrganisasiId == org.Id)
                .OrderBy(a => a.NamaAnggota)
                .ToListAsync();

            return Ok(anggota);
        }

        // ─── POST /api/organisasi/anggota ────────────────────────────────────────
        [HttpPost("anggota")]
        public async Task<IActionResult> TambahAnggota([FromBody] AnggotaRequest req)
        {
            var org = await GetMyOrg();
            if (org == null) return Forbid();

            var anggota = new AnggotaOrganisasi
            {
                OrganisasiId = org.Id,
                NamaAnggota  = req.NamaAnggota,
                NIM          = req.NIM,
                Jabatan      = req.Jabatan,
                IsAktif      = true
            };

            _context.AnggotaOrganisasi.Add(anggota);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAnggota), anggota);
        }

        // ─── PUT /api/organisasi/anggota/{id} ────────────────────────────────────
        [HttpPut("anggota/{id}")]
        public async Task<IActionResult> UpdateAnggota(int id, [FromBody] AnggotaRequest req)
        {
            var org = await GetMyOrg();
            if (org == null) return Forbid();

            var anggota = await _context.AnggotaOrganisasi
                .FirstOrDefaultAsync(a => a.Id == id && a.OrganisasiId == org.Id);

            if (anggota == null) return NotFound(new { error = "Anggota tidak ditemukan." });

            anggota.NamaAnggota = req.NamaAnggota;
            anggota.NIM         = req.NIM;
            anggota.Jabatan     = req.Jabatan;

            await _context.SaveChangesAsync();
            return Ok(anggota);
        }

        // ─── DELETE /api/organisasi/anggota/{id} ─────────────────────────────────
        [HttpDelete("anggota/{id}")]
        public async Task<IActionResult> HapusAnggota(int id)
        {
            var org = await GetMyOrg();
            if (org == null) return Forbid();

            var anggota = await _context.AnggotaOrganisasi
                .FirstOrDefaultAsync(a => a.Id == id && a.OrganisasiId == org.Id);

            if (anggota == null) return NotFound(new { error = "Anggota tidak ditemukan." });

            anggota.IsAktif = false; // soft-deactivate
            await _context.SaveChangesAsync();
            return Ok(new { pesan = "Anggota berhasil dinonaktifkan." });
        }
    }

    public class AnggotaRequest
    {
        public string NamaAnggota { get; set; } = string.Empty;
        public string NIM { get; set; } = string.Empty;
        public string Jabatan { get; set; } = string.Empty;
    }
}
