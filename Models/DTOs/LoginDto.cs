namespace HotelManagementApi.DTOs

{
    // Kullanıcının giriş yaparken API'ye göndereceği bilgileri taşır.
    public class LoginDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}