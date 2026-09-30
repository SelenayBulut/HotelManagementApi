namespace HotelManagementApi.DTOs
{
     // Kullanıcının kendi profil bilgilerini güncellemek için API'ye göndereceği verileri taşır.
    public class UpdateProfileDto
    {
        public string? Username { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; } // Şifresini değiştirmek istemezse boş bırakabilir
    }
}