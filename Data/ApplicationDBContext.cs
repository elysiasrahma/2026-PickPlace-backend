using Microsoft.EntityFrameworkCore;
using PickPlace.Api.Models;

namespace PickPlace.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // 👇 INI YANG TADI KITA TAMBAHKAN
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Filter Otomatis: Jangan ambil data yang sudah dihapus (Soft Delete)
            modelBuilder.Entity<Room>().HasQueryFilter(r => !r.IsDeleted);
            modelBuilder.Entity<Booking>().HasQueryFilter(b => !b.IsDeleted);
        }
    } // <--- Pastikan kurung ini ada (Tutup Class)
} // <--- Pastikan kurung ini ada (Tutup Namespace)