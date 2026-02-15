using System;

namespace PickPlace.Api.Models
{
    public class BookingLog
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string OldStatus { get; set; } = ""; //blm berubah
        public string NewStatus { get; set; } = ""; //baru
        public string? Reason { get; set; } //alasan (opsional)
        public DateTime ChangedAt { get; set; } = DateTime.Now; // kapan berubahnya
    }
}