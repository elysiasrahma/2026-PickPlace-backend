using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;
using PickPlace.Api.Helpers;
using PickPlace.Api.Models;

namespace PickPlace.Api.Controllers
{
    [Route("api/approval")]
    [ApiController]
    [Authorize(Roles = "Dosen")]
    public class ApprovalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── GET /api/approval/pending?page=1&pageSize=10 ────────────────────────
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = JwtHelper.GetUserId(User);
            var dosen  = await _context.Dosen.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dosen == null) return Forbid();

            IQueryable<PeminjamanRuangan> query;

            if (dosen.IsWakilDirektur)
            {
                // Wakil Direktur melihat semua yang sudah melewati Dosen PJ
                query = _context.PeminjamanRuangan
                    .Include(p => p.Organisasi)
                    .Include(p => p.Room)
                    .Where(p => p.Status == "PendingWakilDirektur");
            }
            else
            {
                // Dosen PJ hanya melihat pengajuan dari organisasi yang ia tangani
                query = _context.PeminjamanRuangan
                    .Include(p => p.Organisasi)
                    .Include(p => p.Room)
                    .Where(p =>
                        p.Status == "PendingDosenPJ" &&
                        p.Organisasi!.DosenPJId == dosen.Id);
            }

            var result = await query
                .OrderBy(p => p.TanggalMulai)
                .ToPagedResultAsync(page, pageSize);

            return Ok(result);
        }

        // ─── POST /api/approval/{peminjamanId}/approve ───────────────────────────
        [HttpPost("{peminjamanId}/approve")]
        public async Task<IActionResult> Approve(int peminjamanId)
        {
            var userId = JwtHelper.GetUserId(User);
            var dosen  = await _context.Dosen.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dosen == null) return Forbid();

            var peminjaman = await _context.PeminjamanRuangan
                .Include(p => p.Organisasi).ThenInclude(o => o!.DosenPJ)
                .Include(p => p.Room)
                .FirstOrDefaultAsync(p => p.Id == peminjamanId);

            if (peminjaman == null)
                return NotFound(new { error = "Peminjaman tidak ditemukan." });

            string statusBaru;
            string tipeApproval;
            string pesanBalik;

            if (peminjaman.Status == "PendingDosenPJ" && peminjaman.Organisasi?.DosenPJId == dosen.Id)
            {
                statusBaru   = "PendingWakilDirektur";
                tipeApproval = "DosenPJ";
                pesanBalik   = "Disetujui. Pengajuan diteruskan ke Wakil Direktur.";

                // Notif ke Wakil Direktur
                var wakilDir = await _context.Dosen.FirstOrDefaultAsync(d => d.IsWakilDirektur);
                if (wakilDir != null)
                {
                    await KirimNotifikasiAsync(
                        userId : wakilDir.UserId,
                        judul  : "Persetujuan Peminjaman Menunggu Anda",
                        pesan  : $"Peminjaman #{peminjaman.Id} dari '{peminjaman.Organisasi!.NamaOrganisasi}' " +
                                 $"telah disetujui Dosen PJ. Mohon segera ditinjau.",
                        tipe   : "Approval",
                        refId  : peminjaman.Id
                    );
                }
            }
            else if (peminjaman.Status == "PendingWakilDirektur" && dosen.IsWakilDirektur)
            {
                statusBaru   = "Approved";
                tipeApproval = "WakilDirektur";
                pesanBalik   = "Peminjaman disetujui sepenuhnya dan telah terdaftar.";

                // Daftarkan ke RuanganTerpinjam
                _context.RuanganTerpinjam.Add(new RuanganTerpinjam
                {
                    PeminjamanId   = peminjaman.Id,
                    RoomId         = peminjaman.RoomId,
                    TanggalMulai   = peminjaman.TanggalMulai,
                    TanggalSelesai = peminjaman.TanggalSelesai
                });

                // Notif ke Organisasi
                await KirimNotifikasiAsync(
                    userId : peminjaman.Organisasi!.UserId,
                    judul  : "Peminjaman Disetujui",
                    pesan  : $"Pengajuan peminjaman '{peminjaman.Room?.RoomName}' " +
                             $"pada {peminjaman.TanggalMulai:dd MMM yyyy HH:mm} telah disetujui sepenuhnya.",
                    tipe   : "Approval",
                    refId  : peminjaman.Id
                );
            }
            else
            {
                return BadRequest(new { error = "Anda tidak memiliki akses untuk menyetujui pengajuan ini." });
            }

            // Simpan log
            _context.ApprovalLogs.Add(new ApprovalLog
            {
                PeminjamanId  = peminjaman.Id,
                DosenId       = dosen.Id,
                TipeApproval  = tipeApproval,
                Status        = "Approved",
                DiprosesPada  = DateTime.UtcNow
            });

            peminjaman.Status    = statusBaru;
            peminjaman.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { status = statusBaru, pesan = pesanBalik });
        }

        // ─── POST /api/approval/{peminjamanId}/reject ────────────────────────────
        [HttpPost("{peminjamanId}/reject")]
        public async Task<IActionResult> Reject(int peminjamanId, [FromBody] RejectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Catatan))
                return BadRequest(new { error = "Catatan penolakan wajib diisi." });

            var userId = JwtHelper.GetUserId(User);
            var dosen  = await _context.Dosen.FirstOrDefaultAsync(d => d.UserId == userId);
            if (dosen == null) return Forbid();

            var peminjaman = await _context.PeminjamanRuangan
                .Include(p => p.Organisasi)
                .Include(p => p.Room)
                .FirstOrDefaultAsync(p => p.Id == peminjamanId);

            if (peminjaman == null)
                return NotFound(new { error = "Peminjaman tidak ditemukan." });

            string statusBaru;
            string tipeApproval;

            if (peminjaman.Status == "PendingDosenPJ" && peminjaman.Organisasi?.DosenPJId == dosen.Id)
            {
                statusBaru   = "RejectedDosenPJ";
                tipeApproval = "DosenPJ";
            }
            else if (peminjaman.Status == "PendingWakilDirektur" && dosen.IsWakilDirektur)
            {
                statusBaru   = "RejectedWakilDirektur";
                tipeApproval = "WakilDirektur";
            }
            else
            {
                return BadRequest(new { error = "Anda tidak memiliki akses untuk menolak pengajuan ini." });
            }

            // Simpan log
            _context.ApprovalLogs.Add(new ApprovalLog
            {
                PeminjamanId = peminjaman.Id,
                DosenId      = dosen.Id,
                TipeApproval = tipeApproval,
                Status       = "Rejected",
                Catatan      = req.Catatan,
                DiprosesPada = DateTime.UtcNow
            });

            peminjaman.Status    = statusBaru;
            peminjaman.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Notif ke Organisasi dengan catatan penolakan
            await KirimNotifikasiAsync(
                userId : peminjaman.Organisasi!.UserId,
                judul  : "Pengajuan Ditolak",
                pesan  : $"Pengajuan peminjaman '{peminjaman.Room?.RoomName}' " +
                         $"pada {peminjaman.TanggalMulai:dd MMM yyyy HH:mm} ditolak oleh {tipeApproval}. " +
                         $"Catatan: {req.Catatan}",
                tipe   : "Rejection",
                refId  : peminjaman.Id
            );

            return Ok(new
            {
                status = statusBaru,
                pesan  = "Pengajuan ditolak. Notifikasi telah dikirim ke organisasi."
            });
        }

        // ─── Private Helper ──────────────────────────────────────────────────────
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
    }

    public class RejectRequest
    {
        public string Catatan { get; set; } = string.Empty;
    }
}
