using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class AnggotaOrganisasi
    {
        public int Id { get; set; }

        [Required]
        public int OrganisasiId { get; set; }

        [ForeignKey(nameof(OrganisasiId))]
        public Organisasi? Organisasi { get; set; }

        [Required]
        [MaxLength(150)]
        public string NamaAnggota { get; set; } = string.Empty;

        [MaxLength(20)]
        public string NIM { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Jabatan { get; set; } = string.Empty;

        public bool IsAktif { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
