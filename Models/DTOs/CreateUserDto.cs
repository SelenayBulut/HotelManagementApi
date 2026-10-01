using HotelManagementApi.Models;

namespace HotelManagementApi.DTOs
{
     // Admin tarafından yeni kullanıcı oluşturulurken API'ye gönderilecek verileri taşır.
    public class CreateUserDto
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Customer;

        public decimal Balance { get; set; } = 0.00m;
    }
}