using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class Notifikasi
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string Judul { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Pesan { get; set; } = string.Empty;

        /// <summary>Approval | Rejection | Cancellation | Info</summary>
        [MaxLength(30)]
        public string TipeNotif { get; set; } = "Info";

        /// <summary>Id PeminjamanRuangan terkait (opsional)</summary>
        public int? ReferensiId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
