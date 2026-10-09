using HotelManagementApi.Models; // RoomType enum'ının er erişilebilmesi için
using System;
namespace HotelManagementApi.DTOs
{
    // Yeni oda eklerken dışarıdan alınacak veriler
    public class CreateRoomDto
    {
        public Guid HotelId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public RoomType RoomType { get; set; } = RoomType.Standard;
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? ImageUrl { get; set; }
    }

    // Oda güncellenirken (istenirse sadece değiştirilmek istenen alanlar gönderilsin diye nullable yapıyoruz)
    public class UpdateRoomDto
    {
        public string? RoomNumber { get; set; }
        public RoomType? RoomType { get; set; }
        public int? Capacity { get; set; }
        public decimal? PricePerNight { get; set; }
        public bool? IsAvailable { get; set; }
        public string? ImageUrl { get; set; }
    }
}