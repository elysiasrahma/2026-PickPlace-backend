using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Models;

namespace PickPlace.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Tabel lama
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingLog> BookingLogs { get; set; }

        // Tabel baru
        public DbSet<User> Users { get; set; }
        public DbSet<Dosen> Dosen { get; set; }
        public DbSet<Organisasi> Organisasi { get; set; }
        public DbSet<AnggotaOrganisasi> AnggotaOrganisasi { get; set; }
        public DbSet<PeminjamanRuangan> PeminjamanRuangan { get; set; }
        public DbSet<ApprovalLog> ApprovalLogs { get; set; }
        public DbSet<RuanganTerpinjam> RuanganTerpinjam { get; set; }
        public DbSet<Notifikasi> Notifikasi { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Soft delete filter lama
            modelBuilder.Entity<Room>().HasQueryFilter(r => !r.IsDeleted);
            modelBuilder.Entity<Booking>().HasQueryFilter(b => !b.IsDeleted);

            // Username unik
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // NIP dosen unik
            modelBuilder.Entity<Dosen>()
                .HasIndex(d => d.NIP)
                .IsUnique();

            // User → Dosen (1:1)
            modelBuilder.Entity<Dosen>()
                .HasOne(d => d.User)
                .WithOne(u => u.Dosen)
                .HasForeignKey<Dosen>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User → Organisasi (1:1)
            modelBuilder.Entity<Organisasi>()
                .HasOne(o => o.User)
                .WithOne(u => u.Organisasi)
                .HasForeignKey<Organisasi>(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Organisasi → DosenPJ (N:1) — restrict agar dosen tidak bisa dihapus selama ada org
            modelBuilder.Entity<Organisasi>()
                .HasOne(o => o.DosenPJ)
                .WithMany(d => d.OrganisasiYangDiPJ)
                .HasForeignKey(o => o.DosenPJId)
                .OnDelete(DeleteBehavior.Restrict);

            // PeminjamanRuangan → ApprovalLog (cascade)
            modelBuilder.Entity<ApprovalLog>()
                .HasOne(a => a.Peminjaman)
                .WithMany(p => p.ApprovalLogs)
                .HasForeignKey(a => a.PeminjamanId)
                .OnDelete(DeleteBehavior.Cascade);

            // PeminjamanRuangan → RuanganTerpinjam (cascade)
            modelBuilder.Entity<RuanganTerpinjam>()
                .HasOne(rt => rt.Peminjaman)
                .WithOne(p => p.RuanganTerpinjam)
                .HasForeignKey<RuanganTerpinjam>(rt => rt.PeminjamanId)
                .OnDelete(DeleteBehavior.Cascade);

            // PeminjamanRuangan → CancelledBy (restrict — user tidak bisa dihapus)
            modelBuilder.Entity<PeminjamanRuangan>()
                .HasOne(p => p.CancelledBy)
                .WithMany()
                .HasForeignKey(p => p.CancelledByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}