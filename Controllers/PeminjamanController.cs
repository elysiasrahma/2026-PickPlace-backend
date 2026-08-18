using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Helpers;
using PickPlace.Api.Models;

namespace PickPlace.Api.Controllers
{
    [Route("api/peminjaman")]
    [ApiController]
    [Authorize]
    public class PeminjamanController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private static readonly HashSet<string> CancellableStatuses =
        [
            "Draft", "PendingDosenPJ", "PendingWakilDirektur", "Approved"
        ];

        public PeminjamanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── GET /api/peminjaman ─────────────────────────────────────────────────
        // Query: ?page=1&pageSize=10&status=...&ruanganId=...
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null,
            [FromQuery] int? ruanganId = null)
        {
            var userId = JwtHelper.GetUserId(User);
            var role   = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var query = _context.PeminjamanRuangan
                .Include(p => p.Organisasi)
                .Include(p => p.Room)
                .AsQueryable();

            // Organisasi hanya melihat pengajuan milik mereka sendiri
            if (role == "Organisasi")
            {
                var org = await _context.Organisasi.FirstOrDefaultAsync(o => o.UserId == userId);
                if (org == null) return Forbid();
                query = query.Where(p => p.OrganisasiId == org.Id);
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (ruanganId.HasValue)
                query = query.Where(p => p.RoomId == ruanganId.Value);

            query = query.OrderByDescending(p => p.DiajukanPada);

            var result = await query.ToPagedResultAsync(page, pageSize);
            return Ok(result);
        }

        // ─── GET /api/peminjaman/{id} ────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var p = await _context.PeminjamanRuangan
                .Include(p => p.Organisasi).ThenInclude(o => o!.DosenPJ)
                .Include(p => p.Room)
                .Include(p => p.ApprovalLogs).ThenInclude(a => a.Dosen)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return NotFound(new { error = "Peminjaman tidak ditemukan." });
            return Ok(p);
        }

        // ─── POST /api/peminjaman ────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Organisasi")]
        public async Task<IActionResult> Create([FromBody] CreatePeminjamanRequest req)
        {
            var userId = JwtHelper.GetUserId(User);
            var org    = await _context.Organisasi
                .Include(o => o.DosenPJ)
                .FirstOrDefaultAsync(o => o.UserId == userId);

            if (org == null)
                return BadRequest(new { error = "Akun organisasi tidak ditemukan." });

            // Validasi waktu
            if (req.TanggalMulai >= req.TanggalSelesai)
                return BadRequest(new { error = "TanggalMulai harus lebih awal dari TanggalSelesai." });

            if (req.TanggalMulai <= DateTime.UtcNow)
                return BadRequest(new { error = "Tanggal peminjaman harus di masa mendatang." });

            // Validasi ruangan ada
            var room = await _context.Rooms.FindAsync(req.RoomId);
            if (room == null)
                return BadRequest(new { error = $"Ruangan dengan ID {req.RoomId} tidak ditemukan." });

            // Validasi konflik jadwal
            bool isConflict = await _context.PeminjamanRuangan.AnyAsync(p =>
                p.RoomId == req.RoomId &&
                p.Status != "Cancelled" &&
                p.Status != "RejectedDosenPJ" &&
                p.Status != "RejectedWakilDirektur" &&
                p.TanggalMulai < req.TanggalSelesai &&
                p.TanggalSelesai > req.TanggalMulai);

            if (isConflict)
                return Conflict(new
                {
                    error   = "RoomConflict",
                    pesan   = "Ruangan sudah diajukan/dipinjam pada rentang waktu tersebut."
                });

            var peminjaman = new PeminjamanRuangan
            {
                OrganisasiId     = org.Id,
                RoomId           = req.RoomId,
                TanggalMulai     = req.TanggalMulai,
                TanggalSelesai   = req.TanggalSelesai,
                KeperluanKegiatan = req.KeperluanKegiatan,
                Status           = "PendingDosenPJ",
                DiajukanPada     = DateTime.UtcNow,
                UpdatedAt        = DateTime.UtcNow
            };

            _context.PeminjamanRuangan.Add(peminjaman);
            await _context.SaveChangesAsync();

            // Notifikasi ke Dosen PJ
            await KirimNotifikasiAsync(
                userId   : org.DosenPJ!.UserId,
                judul    : "Pengajuan Peminjaman Baru",
                pesan    : $"Organisasi '{org.NamaOrganisasi}' mengajukan peminjaman ruangan '{room.RoomName}' " +
                           $"pada {req.TanggalMulai:dd MMM yyyy HH:mm}. Mohon segera ditinjau.",
                tipe     : "Approval",
                refId    : peminjaman.Id
            );

            return CreatedAtAction(nameof(GetById), new { id = peminjaman.Id }, new
            {
                peminjaman.Id,
                peminjaman.Status,
                pesan = "Pengajuan berhasil dikirim. Menunggu persetujuan Dosen Penanggung Jawab."
            });
        }

        // ─── POST /api/peminjaman/{id}/cancel ───────────────────────────────────
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.AlasanPembatalan))
                return BadRequest(new { error = "AlasanPembatalan wajib diisi." });

            var peminjaman = await _context.PeminjamanRuangan
                .Include(p => p.Organisasi).ThenInclude(o => o!.DosenPJ)
                .Include(p => p.RuanganTerpinjam)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (peminjaman == null)
                return NotFound(new { error = "Peminjaman tidak ditemukan." });

            if (!CancellableStatuses.Contains(peminjaman.Status))
                return UnprocessableEntity(new
                {
                    error = "InvalidStatusForCancellation",
                    pesan = $"Pengajuan dengan status '{peminjaman.Status}' tidak dapat dibatalkan."
                });

            var userId = JwtHelper.GetUserId(User);
            var role   = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var isAdmin = role == "Admin";

            // Non-admin: wajib H-2 sebelum acara
            if (!isAdmin)
            {
                var batas = peminjaman.TanggalMulai.AddDays(-2);
                if (DateTime.UtcNow >= batas)
                    return Conflict(new
                    {
                        error           = "CancellationWindowExpired",
                        pesan           = "Pembatalan tidak dapat dilakukan. Batas waktu pembatalan adalah H-2 sebelum acara.",
                        batasPembatalan = batas,
                        tanggalMulai    = peminjaman.TanggalMulai
                    });
            }

            var statusSebelumnya = peminjaman.Status;

            peminjaman.Status             = "Cancelled";
            peminjaman.CancelledAt        = DateTime.UtcNow;
            peminjaman.CancelledByUserId  = userId;
            peminjaman.AlasanPembatalan   = req.AlasanPembatalan;
            peminjaman.UpdatedAt          = DateTime.UtcNow;

            // Hapus dari RuanganTerpinjam jika sudah Approved
            if (statusSebelumnya == "Approved" && peminjaman.RuanganTerpinjam != null)
                _context.RuanganTerpinjam.Remove(peminjaman.RuanganTerpinjam);

            await _context.SaveChangesAsync();

            // Kirim notifikasi ke semua pihak yang terlibat
            await KirimNotifikasiPembatalanAsync(peminjaman, statusSebelumnya, userId, req.AlasanPembatalan, isAdmin);

            return Ok(new
            {
                peminjaman.Id,
                peminjaman.Status,
                cancelledAt = peminjaman.CancelledAt,
                pesan       = "Peminjaman berhasil dibatalkan. Notifikasi telah dikirim ke pihak terkait."
            });
        }

        // ─── Private Helpers ─────────────────────────────────────────────────────

        private async Task KirimNotifikasiAsync(int userId, string judul, string pesan, string tipe, int? refId = null)
        {
            _context.Notifikasi.Add(new Notifikasi
            {
                UserId      = userId,
                Judul       = judul,
                Pesan       = pesan,
                TipeNotif   = tipe,
                ReferensiId = refId
            });
            await _context.SaveChangesAsync();
        }

        private async Task KirimNotifikasiPembatalanAsync(
            PeminjamanRuangan peminjaman,
            string statusSebelumnya,
            int cancellerUserId,
            string alasan,
            bool isAdmin)
        {
            var judulNotif = isAdmin
                ? $"Pembatalan oleh Admin — {alasan}"
                : "Peminjaman Dibatalkan";

            var penerima = new List<int>();

            // Tambah user organisasi
            var orgUser = await _context.Organisasi
                .Where(o => o.Id == peminjaman.OrganisasiId)
                .Select(o => o.UserId)
                .FirstOrDefaultAsync();
            if (orgUser != 0 && orgUser != cancellerUserId)
                penerima.Add(orgUser);

            // Tambah Dosen PJ jika sudah melewati Draft
            if (statusSebelumnya != "Draft")
            {
                var dosenPJUser = await _context.Organisasi
                    .Include(o => o.DosenPJ)
                    .Where(o => o.Id == peminjaman.OrganisasiId)
                    .Select(o => o.DosenPJ!.UserId)
                    .FirstOrDefaultAsync();
                if (dosenPJUser != 0 && dosenPJUser != cancellerUserId)
                    penerima.Add(dosenPJUser);
            }

            // Tambah Wakil Direktur jika sudah di tahap PendingWakilDirektur / Approved
            if (statusSebelumnya == "PendingWakilDirektur" || statusSebelumnya == "Approved")
            {
                var wakilDirUser = await _context.Dosen
                    .Where(d => d.IsWakilDirektur)
                    .Select(d => d.UserId)
                    .FirstOrDefaultAsync();
                if (wakilDirUser != 0 && wakilDirUser != cancellerUserId)
                    penerima.Add(wakilDirUser);
            }

            var pesanNotif = $"Peminjaman #{peminjaman.Id} ({peminjaman.TanggalMulai:dd MMM yyyy}) " +
                             $"telah dibatalkan. Alasan: {alasan}";

            var notifikasi = penerima.Distinct().Select(uid => new Notifikasi
            {
                UserId      = uid,
                Judul       = judulNotif,
                Pesan       = pesanNotif,
                TipeNotif   = "Cancellation",
                ReferensiId = peminjaman.Id
            });

            _context.Notifikasi.AddRange(notifikasi);
            await _context.SaveChangesAsync();
        }
    }

    public class CreatePeminjamanRequest
    {
        public int RoomId { get; set; }
        public DateTime TanggalMulai { get; set; }
        public DateTime TanggalSelesai { get; set; }
        public string KeperluanKegiatan { get; set; } = string.Empty;
    }

    public class CancelRequest
    {
        public string AlasanPembatalan { get; set; } = string.Empty;
    }
}
