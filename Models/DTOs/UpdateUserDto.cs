using HotelManagementApi.Models;

namespace HotelManagementApi.DTOs
{
    // Admin tarafından mevcut bir kullanıcının bilgilerini güncellemek için kullanılır.
    public class UpdateUserDto
    {
        public string? Username { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public UserRole? Role { get; set; }

        public decimal? Balance { get; set; }
    }
}