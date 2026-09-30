namespace HotelManagementApi.DTOs
{
    // Yeni rezervasyon oluşturulurken API'ye gönderilecek bilgileri taşır.
    public class CreateReservationDto
    {
        public int RoomId { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int GuestCount { get; set; }
    }
}