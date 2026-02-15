using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PickPlace.Api.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Nama Peminjam wajib diisi!")]
        public string BorrowerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Asal Organisasi wajib diisi!")]
        public string Organization { get; set; } = string.Empty;

        [Required(ErrorMessage = "ID Ruangan wajib diisi!")]
        public int RoomId { get; set; }

        public Room? Room { get; set; }

        [Required(ErrorMessage = "Waktu Mulai wajib diisi!")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Waktu Selesai wajib diisi!")]
        public DateTime EndTime { get; set; }

        [Required(ErrorMessage = "Keperluan peminjaman wajib diisi!")]
        public string Purpose { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public string? RejectReason { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}