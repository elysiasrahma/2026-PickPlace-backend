using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class Organisasi
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        [MaxLength(200)]
        public string NamaOrganisasi { get; set; } = string.Empty;

        /// <summary>Dosen Penanggung Jawab organisasi ini</summary>
        [Required]
        public int DosenPJId { get; set; }

        [ForeignKey(nameof(DosenPJId))]
        public Dosen? DosenPJ { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigasi balik
        public ICollection<AnggotaOrganisasi> Anggota { get; set; } = [];
    }
}
