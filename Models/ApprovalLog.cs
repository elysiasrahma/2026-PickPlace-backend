using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class ApprovalLog
    {
        public int Id { get; set; }

        [Required]
        public int PeminjamanId { get; set; }

        [ForeignKey(nameof(PeminjamanId))]
        public PeminjamanRuangan? Peminjaman { get; set; }

        [Required]
        public int DosenId { get; set; }

        [ForeignKey(nameof(DosenId))]
        public Dosen? Dosen { get; set; }

        /// <summary>DosenPJ | WakilDirektur</summary>
        [Required]
        [MaxLength(20)]
        public string TipeApproval { get; set; } = string.Empty;

        /// <summary>Approved | Rejected</summary>
        [Required]
        [MaxLength(15)]
        public string Status { get; set; } = string.Empty;

        /// <summary>Wajib diisi saat menolak</summary>
        [MaxLength(1000)]
        public string? Catatan { get; set; }

        public DateTime DiprosesPada { get; set; } = DateTime.UtcNow;
    }
}
