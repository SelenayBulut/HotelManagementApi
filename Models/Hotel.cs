using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelManagementApi.Models
{

    // Sistemdeki otelleri ve otel sahipleriyle olan ilişkilerini temsil eder.
    public class Hotel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OwnerId { get; set; } // Oteli oluşturan kullanıcının ID'si (Foreign Key)

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty; // Şehir alanı

       
        public decimal Rating { get; set; } = 0.0m; // Puan alanı

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (İlişkiler)
        [ForeignKey("OwnerId")]
        public User Owner { get; set; } = null!; // Bu otelin sahibi olan kullanıcı

        public ICollection<Room> Rooms { get; set; } = new List<Room>(); // Otelin odaları
    }
}