using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class RuanganTerpinjam
    {
        public int Id { get; set; }

        [Required]
        public int PeminjamanId { get; set; }

        [ForeignKey(nameof(PeminjamanId))]
        public PeminjamanRuangan? Peminjaman { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; } // optional agar tidak konflik dengan global query filter Room

        public DateTime TanggalMulai { get; set; }
        public DateTime TanggalSelesai { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
