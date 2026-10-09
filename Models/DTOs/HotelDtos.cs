namespace HotelManagementApi.DTOs
{

     // Otel oluşturma ve güncelleme işlemlerinde API'ye gönderilecek verileri taşır.

    // Yeni bir otel oluştururken kullanılacak verileri tanımlar.
    public class CreateHotelDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty; 
        public decimal Rating { get; set; } 
        public string? Description { get; set; } //? null olabilir demek nullable demekk
        public string? ImageUrl { get; set; }
    }

    // Mevcut bir oteli güncellerken değiştirilecek alanları tanımlar.
    public class UpdateHotelDto
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; } 
        public decimal? Rating { get; set; } 
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }
}