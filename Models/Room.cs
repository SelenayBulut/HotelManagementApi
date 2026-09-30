using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace HotelManagementApi.Models
{
     // Sistemdeki otel odalarını temsil eden Entity sınıfıdır.
    public class Room
    {
        [Key]
        public int Id { get; set; }
        public int HotelId { get; set; }
        
        [JsonIgnore] // Döngüsel hatayı önlemek için hotel room ilşikisinde
        public Hotel? Hotel { get; set; }

        public string RoomNumber { get; set; } = string.Empty;
        
        // Enum kullanımı: Dışarıdan sadece bu 3 değer seçilebilir
        [JsonConverter(typeof(JsonStringEnumConverter))] // Swagger'da sayı yerine yazı ("Standard", "Deluxe") 
        public RoomType RoomType { get; set; } = RoomType.Standard;
        
        public int Capacity { get; set; }                  // Kişi kapasitesi
        public decimal PricePerNight { get; set; }          //Fiyatlandırma
        public bool IsAvailable { get; set; } = true;      // Müsaitlik durumu
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>(); // 1 to N (yani bir odanın birden fazla rezervasyonu olabilir.)
    }
}