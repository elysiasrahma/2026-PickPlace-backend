using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Data;

namespace PickPlace.Api.BackgroundServices
{
    /// <summary>
    /// Menghapus semua riwayat peminjaman yang TanggalSelesai-nya sudah lewat lebih dari 1 bulan.
    /// Berjalan setiap hari pukul 00:00 server time.
    /// </summary>
    public class CleanupBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CleanupBackgroundService> _logger;

        public CleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<CleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[Cleanup] Service dimulai.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Hitung delay sampai tengah malam berikutnya
                var now     = DateTime.Now;
                var nextRun = now.Date.AddDays(1); // besok 00:00:00
                var delay   = nextRun - now;

                _logger.LogInformation("[Cleanup] Job berikutnya dijadwalkan pukul {NextRun}.", nextRun);
                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                    await RunCleanupAsync(stoppingToken);
            }
        }

        private async Task RunCleanupAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Hapus semua record yang TanggalSelesai < sekarang - 1 bulan
                // Cascade delete akan ikut menghapus ApprovalLog & RuanganTerpinjam terkait
                var batas = DateTime.UtcNow.AddMonths(-1);

                var toDelete = await db.PeminjamanRuangan
                    .Where(p => p.TanggalSelesai < batas)
                    .ToListAsync(ct);

                if (toDelete.Count > 0)
                {
                    db.PeminjamanRuangan.RemoveRange(toDelete);
                    await db.SaveChangesAsync(ct);

                    _logger.LogInformation(
                        "[Cleanup] {Count} riwayat peminjaman dihapus (TanggalSelesai < {Batas:yyyy-MM-dd}).",
                        toDelete.Count, batas);
                }
                else
                {
                    _logger.LogInformation("[Cleanup] Tidak ada riwayat yang perlu dihapus hari ini.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[Cleanup] Error saat menjalankan cleanup riwayat peminjaman.");
            }
        }
    }
}
