using System.ComponentModel.DataAnnotations;

namespace HotelManagementApi.DTOs
{
    // Rezervasyon güncellenirken API'ye gönderilecek bilgileri taşır.
    public class UpdateReservationDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int RoomId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        [Required]
        public int GuestCount { get; set; }
    }
}