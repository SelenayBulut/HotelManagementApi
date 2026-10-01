using System.ComponentModel.DataAnnotations;
using System;
namespace HotelManagementApi.DTOs
{
    // Rezervasyon güncellenirken API'ye gönderilecek bilgileri taşır.
    public class UpdateReservationDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public int GuestCount { get; set; }
    }
}