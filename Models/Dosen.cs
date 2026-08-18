using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class Dosen
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(20)]
        public string NIP { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string NamaLengkap { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        /// <summary>True jika dosen ini menjabat sebagai Wakil Direktur</summary>
        public bool IsWakilDirektur { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigasi balik
        public ICollection<Organisasi> OrganisasiYangDiPJ { get; set; } = [];
    }
}
