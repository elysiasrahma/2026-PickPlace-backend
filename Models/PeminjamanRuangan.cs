using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class PeminjamanRuangan
    {
        public int Id { get; set; }

        [Required]
        public int OrganisasiId { get; set; }

        [ForeignKey(nameof(OrganisasiId))]
        public Organisasi? Organisasi { get; set; }

        [Required]
        public int RoomId { get; set; }

        [ForeignKey(nameof(RoomId))]
        public Room? Room { get; set; }

        [Required]
        public DateTime TanggalMulai { get; set; }

        [Required]
        public DateTime TanggalSelesai { get; set; }

        [Required]
        [MaxLength(500)]
        public string KeperluanKegiatan { get; set; } = string.Empty;

        /// <summary>
        /// Draft | PendingDosenPJ | PendingWakilDirektur | Approved |
        /// RejectedDosenPJ | RejectedWakilDirektur | Cancelled
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "Draft";

        public DateTime DiajukanPada { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Kolom Pembatalan ---
        public DateTime? CancelledAt { get; set; }
        public int? CancelledByUserId { get; set; }

        [ForeignKey(nameof(CancelledByUserId))]
        public User? CancelledBy { get; set; }

        /// <summary>Wajib diisi saat pembatalan oleh siapapun</summary>
        [MaxLength(500)]
        public string? AlasanPembatalan { get; set; }

        // Navigasi
        public ICollection<ApprovalLog> ApprovalLogs { get; set; } = [];
        public RuanganTerpinjam? RuanganTerpinjam { get; set; }
    }
}
