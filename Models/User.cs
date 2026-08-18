using System.ComponentModel.DataAnnotations;

namespace PickPlace.Api.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>Admin | Dosen | Organisasi</summary>
        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigasi
        public Dosen? Dosen { get; set; }
        public Organisasi? Organisasi { get; set; }
    }
}
