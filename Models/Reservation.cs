using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HotelManagementApi.Models
{
    // Sistemdeki rezervasyonları temsil eden Entity sınıfıdır.
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public int GuestCount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }


       // Rezervasyon iptal edildiğinde uygulanabilecek ceza tutarıdır.
        [Column(TypeName = "decimal(18,2)")]
        public decimal PenaltyFee { get; set; } = 0.00m;

         // JSON çıktısında enum değerlerinin sayı yerine metin olarak görünmesini sağlar.
        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties reservation ile arasındaki ilişkiler için
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }
    }
}